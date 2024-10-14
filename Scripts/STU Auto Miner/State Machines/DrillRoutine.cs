using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class DrillRoutine : STUStateMachine {

            public override string Name => "Drill Routine";

            public enum RunStates {
                ORIENT_AGAINST_JOB_PLANE,
                ASSIGN_SILO_START,
                FLY_TO_SILO_START,
                EXTRACT_SILO,
                PULL_OUT,
                RTB
            }

            STUFlightController FlightController { get; set; }
            RunStates RunState { get; set; }
            List<IMyShipDrill> Drills { get; set; }
            Vector3 JobSite { get; set; }
            PlaneD JobPlane { get; set; }
            STUInventoryEnumerator InventoryEnumerator;

            Dictionary<string, double> ItemCounts;

            int CurrentSilo = 0;

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
                Silos = GetSilos(JobSite, JobPlane, 3, Drills[0]);
                Drills.ForEach(drill => drill.Enabled = true);
                return true;
            }

            public override bool Closeout() {
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
                ItemCounts = InventoryEnumerator.GetItemTotals();

                //double hydrogen = ItemCounts.ContainsKey("Hydrogen") ? ItemCounts["Hydrogen"] : 0;
                //double power = ItemCounts.ContainsKey("Power") ? ItemCounts["Power"] : 0;

                //if (HydrogenRunningLow(InventoryEnumerator.HydrogenCapacity, hydrogen)) {
                //    RunState = RunStates.RTB;
                //}

                //if (BatteriesRunningLow(InventoryEnumerator.PowerCapacity, power)) {
                //    RunState = RunStates.RTB;
                //}

                switch (RunState) {

                    case RunStates.ORIENT_AGAINST_JOB_PLANE:
                        Vector3D closestPointOnJobPlane = GetClosestPointOnJobPlane(JobPlane, FlightController.CurrentPosition);
                        bool aligned = FlightController.AlignShipToTarget(closestPointOnJobPlane);
                        FlightController.SetStableForwardVelocity(0);
                        if (aligned) {
                            CreateOkBroadcast("Oriented against job plane, flying to first silo");
                            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].StartPos, 1);
                            RunState = RunStates.FLY_TO_SILO_START;
                        }
                        break;

                    case RunStates.FLY_TO_SILO_START:
                        aligned = FlightController.AlignShipToTarget(Silos[CurrentSilo].EndPos);
                        bool finishedGoToManeuver = FlightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver && aligned) {
                            RunState = RunStates.EXTRACT_SILO;
                            CreateOkBroadcast("Arrived at silo start, starting extraction");
                            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].EndPos, 0.3);
                        }
                        break;

                    case RunStates.EXTRACT_SILO:
                        finishedGoToManeuver = FlightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver) {
                            CreateOkBroadcast("Finished extracting silo; starting to pull out");
                            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].StartPos, 1);
                            RunState = RunStates.PULL_OUT;
                        }
                        break;

                    case RunStates.PULL_OUT:
                        finishedGoToManeuver = FlightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (finishedGoToManeuver) {
                            CurrentSilo++;
                            if (CurrentSilo >= Silos.Count) {
                                RunState = RunStates.RTB;
                            } else {
                                CreateOkBroadcast("Finished pulling out; flying to next silo");
                                FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, Silos[CurrentSilo].StartPos, 1);
                                RunState = RunStates.FLY_TO_SILO_START;
                            }
                        }
                        break;

                    case RunStates.RTB:
                        return true;

                }

                return false;

            }

            private bool HydrogenRunningLow(float capacity, double currentHydrogen) {
                // TODO: Come up with more sophisticated way to determine if hydrogen is running low
                return currentHydrogen / capacity < 0.25;
            }

            private bool BatteriesRunningLow(float capacity, double currentPower) {
                // TODO: Come up with more sophisticated way to determine if batteries are running low
                return currentPower / capacity < 0.25;
            }

            Vector3D GetClosestPointOnJobPlane(PlaneD jobPlane, Vector3D currentPos) {
                return jobPlane.ProjectPoint(ref currentPos);
            }


            private List<Silo> GetSilos(Vector3D jobSite, PlaneD jobPlane, int n, IMyTerminalBlock referenceBlock) {

                double shipWidth = referenceBlock.CubeGrid.WorldAABB.Size.X;
                double shipHeight = referenceBlock.CubeGrid.WorldAABB.Size.Y;
                double shipLength = referenceBlock.CubeGrid.WorldAABB.Size.Z;

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
                        Vector3D offset = right * (i - halfGridSize) * shipWidth + up * (j - halfGridSize) * shipHeight;
                        Vector3D startPos = jobSite + offset;
                        Vector3D endPos = startPos + normal * siloDepth;

                        Vector3D endToStart = startPos - endPos;
                        endToStart.Normalize();
                        startPos += endToStart * (shipLength + 5);

                        silos.Add(new Silo(startPos, endPos));
                    }
                }

                return silos;
            }
        }
    }
}
