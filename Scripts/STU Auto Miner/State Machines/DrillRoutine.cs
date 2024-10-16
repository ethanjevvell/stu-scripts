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

            STUFlightController FlightController { get; set; }
            RunStates RunState { get; set; }
            List<IMyShipDrill> Drills { get; set; }
            Vector3 JobSite { get; set; }
            PlaneD JobPlane { get; set; }
            STUInventoryEnumerator InventoryEnumerator;

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
                FlightController.ReinstateGyroControl();
                FlightController.ReinstateThrusterControl();
                RunState = RunStates.ORIENT_AGAINST_JOB_PLANE;
                Silos = GetSilos(JobSite, JobPlane, 3, FlightController.RemoteControl);
                Drills.ForEach(drill => drill.Enabled = true);
                return true;
            }

            public override bool Closeout() {
                Drills.ForEach(drill => drill.Enabled = false);
                return true;
            }

            public DrillRoutine(STUFlightController fc, List<IMyShipDrill> drills, Vector3 jobSite, PlaneD jobPlane, STUInventoryEnumerator inventoryEnumerator) {

                FlightController = fc;
                JobSite = jobSite;
                JobPlane = jobPlane;
                Drills = drills;
                InventoryEnumerator = inventoryEnumerator;

                if (JobPlane == null) {
                    throw new Exception("Job plane is null");
                }

            }

            public override bool Run() {

                // Constant mass updates to account for drilling
                FlightController.UpdateShipMass();

                switch (RunState) {

                    case RunStates.ORIENT_AGAINST_JOB_PLANE:
                        Vector3D closestPointOnJobPlane = GetClosestPointOnJobPlane(JobPlane, FlightController.CurrentPosition);
                        bool aligned = FlightController.AlignShipToTarget(closestPointOnJobPlane);
                        FlightController.SetStableForwardVelocity(0);
                        if (aligned) {
                            CreateOkBroadcast("Oriented against job plane, flying to first silo");
                            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].StartPos, 5);
                            RunState = RunStates.FLY_TO_SILO_START;
                        }
                        break;

                    case RunStates.FLY_TO_SILO_START:
                        aligned = FlightController.AlignShipToTarget(Silos[CurrentSilo].EndPos);
                        bool finishedGoToManeuver = FlightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver && aligned) {
                            RunState = RunStates.EXTRACT_SILO;
                            CreateOkBroadcast("Arrived at silo start, starting extraction");
                            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].EndPos, 1);
                        }
                        break;

                    case RunStates.EXTRACT_SILO:
                        if (StorageIsFull()) {
                            CreateOkBroadcast("Storage is full; returning to base");
                            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].StartPos, 3);
                            RunState = RunStates.PULL_OUT_UNFINISHED_SILO;
                            break;
                        }
                        finishedGoToManeuver = FlightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver) {
                            CreateOkBroadcast("Finished extracting silo; starting to pull out");
                            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].StartPos, 3);
                            RunState = RunStates.PULL_OUT_FINISHED_SILO;
                        }
                        break;

                    case RunStates.PULL_OUT_UNFINISHED_SILO:
                        finishedGoToManeuver = FlightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver) {
                            CreateOkBroadcast("Finished pulling out; returning to base");
                            RunState = RunStates.RTB_BUT_NOT_FINISHED;
                        }
                        break;

                    case RunStates.PULL_OUT_FINISHED_SILO:
                        finishedGoToManeuver = FlightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver) {
                            CurrentSilo++;
                            if (CurrentSilo >= Silos.Count) {
                                RunState = RunStates.FINISHED_JOB;
                            } else {
                                CreateOkBroadcast("Finished pulling out; flying to next silo");
                                FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].StartPos, 3);
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
                return InventoryEnumerator.GetFilledRatio() >= 0.95;
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
                double siloDepth = shipLength * 1; // Adjust as needed

                List<Silo> silos = new List<Silo>();
                for (int i = 0; i < n; i++) {
                    for (int j = 0; j < n; j++) {
                        Vector3D offset = right * (i - halfGridSize) * shipWidth + up * (j - halfGridSize) * shipWidth;
                        Vector3D startPos = jobSite + offset;
                        Vector3D endPos = startPos + normal * siloDepth;

                        Vector3D endToStart = startPos - endPos;
                        endToStart.Normalize();
                        startPos += endToStart * shipLength;

                        silos.Add(new Silo(startPos, endPos));
                    }
                }

                return silos;
            }
        }
    }
}
