using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class DrillRoutine : STUStateMachine {

            public override string Name => "Drill Routine";
            public bool FinishedLastJob { get; private set; }

            public enum RunStates {
                ORIENT_AGAINST_JOB_PLANE,
                ASSIGN_SILO_START,
                FLY_TO_SILO_START,
                EXTRACT_SILO,
                PULL_OUT_FINISHED_SILO,
                PULL_OUT_UNFINISHED_SILO,
                FINISHED_JOB,
                RTB_BUT_NOT_FINISHED
            }

            STUFlightController _flightController { get; set; }
            RunStates RunState { get; set; }
            List<IMyShipDrill> _drills { get; set; }
            Vector3 _jobSite { get; set; }
            PlaneD _jobPlane { get; set; }
            int _jobDepth { get; set; }
            STUInventoryEnumerator _inventoryEnumerator;

            public int CurrentSilo = 0;

            class Silo {
                public Vector3D StartPos;
                public Vector3D EndPos;
                public Silo(Vector3D startPos, Vector3D endPos) {
                    StartPos = startPos;
                    EndPos = endPos;
                }
            }

            List<Silo> Silos { get; set; }

            public override bool Init() {
                _flightController.ReinstateGyroControl();
                _flightController.ReinstateThrusterControl();
                RunState = RunStates.ORIENT_AGAINST_JOB_PLANE;
                Silos = GetSilos(_jobSite, _jobPlane, 3, _flightController.RemoteControl);
                return true;
            }

            public override bool Closeout() {
                _drills.ForEach(drill => drill.Enabled = false);
                _flightController.UpdateShipMass();
                return true;
            }

            public DrillRoutine(STUFlightController flightController, List<IMyShipDrill> drills, Vector3 jobSite, PlaneD jobPlane, int jobDepth, STUInventoryEnumerator inventoryEnumerator) {

                _flightController = flightController;
                _jobSite = jobSite;
                _jobPlane = jobPlane;
                _jobDepth = jobDepth;
                _drills = drills;
                _inventoryEnumerator = inventoryEnumerator;

                if (_jobPlane == null) {
                    throw new Exception("Job plane is null");
                }

            }

            public override bool Run() {

                // Constant mass updates to account for drilling
                _flightController.UpdateShipMass();

                switch (RunState) {

                    case RunStates.ORIENT_AGAINST_JOB_PLANE:
                        Vector3D closestPointOnJobPlane = GetClosestPointOnJobPlane(_jobPlane, _flightController.CurrentPosition);
                        bool aligned = _flightController.AlignShipToTarget(closestPointOnJobPlane);
                        _flightController.SetStableForwardVelocity(0);
                        if (aligned) {
                            _flightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(_flightController, Silos[CurrentSilo].StartPos, 5);
                            RunState = RunStates.FLY_TO_SILO_START;
                            _drills.ForEach(drill => drill.Enabled = true);
                        }
                        break;

                    case RunStates.FLY_TO_SILO_START:
                        aligned = _flightController.AlignShipToTarget(Silos[CurrentSilo].EndPos);
                        _flightController.GotoAndStopManeuver.CruiseVelocity = Vector3D.Distance(_flightController.CurrentPosition, Silos[CurrentSilo].StartPos) < 100 ? 5 : 20;
                        bool finishedGoToManeuver = _flightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver && aligned) {
                            RunState = RunStates.EXTRACT_SILO;
                            CreateInfoBroadcast("Arrived at silo start, starting extraction");
                            _flightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(_flightController, Silos[CurrentSilo].EndPos, 1);
                            _drills.ForEach(drill => drill.Enabled = true);
                        }
                        break;

                    case RunStates.EXTRACT_SILO:
                        if (StorageIsFull()) {
                            CreateInfoBroadcast("Storage full");
                            _flightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(_flightController, Silos[CurrentSilo].StartPos, 3);
                            RunState = RunStates.PULL_OUT_UNFINISHED_SILO;
                            _drills.ForEach(drill => drill.Enabled = false);
                            break;
                        }
                        finishedGoToManeuver = _flightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver) {
                            CreateInfoBroadcast("Finished extracting silo; starting to pull out");
                            _flightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(_flightController, Silos[CurrentSilo].StartPos, 3);
                            RunState = RunStates.PULL_OUT_FINISHED_SILO;
                            _drills.ForEach(drill => drill.Enabled = false);
                        }
                        break;

                    case RunStates.PULL_OUT_UNFINISHED_SILO:
                        finishedGoToManeuver = _flightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver) {
                            CreateOkBroadcast("Returning to base");
                            RunState = RunStates.RTB_BUT_NOT_FINISHED;
                        }
                        break;

                    case RunStates.PULL_OUT_FINISHED_SILO:
                        finishedGoToManeuver = _flightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver) {
                            CurrentSilo++;
                            if (CurrentSilo >= Silos.Count) {
                                RunState = RunStates.FINISHED_JOB;
                            } else {
                                CreateInfoBroadcast("Finished pulling out; flying to next silo");
                                _flightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(_flightController, Silos[CurrentSilo].StartPos, 3);
                                RunState = RunStates.FLY_TO_SILO_START;
                            }
                        }
                        break;

                    case RunStates.FINISHED_JOB:
                        CreateOkBroadcast("Completely finished job, returning to base");
                        FinishedLastJob = true;
                        return true;

                    case RunStates.RTB_BUT_NOT_FINISHED:
                        CreateOkBroadcast("Returning to base for refueling");
                        FinishedLastJob = false;
                        return true;

                }

                return false;

            }

            private bool StorageIsFull() {
                return _inventoryEnumerator.FilledRatio >= 0.95;
            }

            Vector3D GetClosestPointOnJobPlane(PlaneD jobPlane, Vector3D currentPos) {
                return jobPlane.ProjectPoint(ref currentPos);
            }


            private List<Silo> GetSilos(Vector3D jobSite, PlaneD jobPlane, int n, IMyTerminalBlock referenceBlock) {

                Vector3D shipDimensions = (referenceBlock.CubeGrid.Max - referenceBlock.CubeGrid.Min + Vector3I.One) * referenceBlock.CubeGrid.GridSize;

                double shipWidth = shipDimensions.X;
                // For unknown reasons, the ship length is actually the Y-dimension... should investigate further some day
                double shipLength = shipDimensions.Y;

                // Get the normal of the job plane
                Vector3D normal = Vector3D.Normalize(jobPlane.Normal);

                // Handle the case where normal is parallel to Vector3D.Up
                Vector3D upVector = Vector3D.Up;
                if (Vector3D.IsZero(Vector3D.Cross(normal, upVector))) {
                    upVector = Vector3D.Right; // Use an alternative vector
                }
                Vector3D right = Vector3D.Normalize(Vector3D.Cross(normal, upVector));
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(right, normal));

                double halfGridSize = (n - 1) / 2.0;
                double siloDepth = _jobDepth;

                List<Silo> silos = new List<Silo>();
                for (int i = 0; i < n; i++) {
                    for (int j = 0; j < n; j++) {
                        Vector3D offset = right * (i - halfGridSize) * shipWidth + up * (j - halfGridSize) * shipWidth;
                        Vector3D startPos = jobSite + offset;
                        Vector3D endPos = startPos + normal * siloDepth;

                        Vector3D endToStart = startPos - endPos;
                        endToStart.Normalize();
                        // Adding 5 to offset odd Crait issue
                        startPos += endToStart * (shipLength + 5);

                        silos.Add(new Silo(startPos, endPos));
                    }
                }

                return silos;
            }
        }
    }
}
