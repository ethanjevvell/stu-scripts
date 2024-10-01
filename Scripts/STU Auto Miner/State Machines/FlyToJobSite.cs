using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public partial class FlyToJobSite : STUStateMachine {

            public enum RunStates {
                ASCEND,
                CRUISE,
                DESCEND,
            }

            STUFlightController FlightController { get; set; }
            IMyShipConnector Connector { get; set; }
            List<IMyGasTank> HydrogenTanks { get; set; }
            List<IMyBatteryBlock> Batteries { get; set; }
            RunStates RunState { get; set; }
            Vector3[] CruiseWaypoints { get; set; }
            int CurrentWaypoint { get; set; }
            public Vector3 JobSite { get; set; }

            public FlyToJobSite(STUFlightController fc, IMyShipConnector connector, List<IMyGasTank> hydrogenTanks, List<IMyBatteryBlock> batteries) {
                FlightController = fc;
                Connector = connector;
                HydrogenTanks = hydrogenTanks;
                Batteries = batteries;
                RunState = RunStates.ASCEND;
            }

            public override string Name => "Fly To Job Site";

            public override bool Init() {
                Connector.Disconnect();
                HydrogenTanks.ForEach(tank => tank.Stockpile = true);
                Batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Auto);
                if (JobSite == null) {
                    CreateFatalErrorBroadcast("No job site loaded into memory; did you forget to set this.JobSite?");
                }
                CruiseWaypoints = CalculateCruiseWaypoints(JobSite);
                if (CruiseWaypoints == null || CruiseWaypoints.Length == 0) {
                    CreateFatalErrorBroadcast($"Failed to calculate cruise waypoints; cruise waypoints length: {CruiseWaypoints.Length}");
                }
                return true;
            }

            public override bool Closeout() {
                // TODO
                return true;
            }

            Vector3[] CalculateCruiseWaypoints(Vector3 jobSite) {
                // Get current planet
                Vector3 currentLocation = FlightController.CurrentPosition;
                Vector3 planetCenter = STUGalacticMap.GetPlanetOfPoint(currentLocation).Value.Center;

                Vector3 PC = currentLocation - planetCenter;
                Vector3 PJ = jobSite - planetCenter;

                double radius1 = PC.Length();
                double radius2 = PJ.Length();

                PC.Normalize();
                PJ.Normalize();

                Vector3 rotationAxis = Vector3.Cross(PC, PJ);
                double angle = Math.Acos(Vector3.Dot(PC, PJ));

                Vector3[] waypoints = new Vector3[10];
                for (int i = 0; i < waypoints.Length; i++) {
                    double t = (double)i / (waypoints.Length - 1); // Ensures inclusion of endpoint
                    double radius = radius1 + t * (radius2 - radius1);
                    Vector3D rotatedPC = Vector3D.Transform(PC, MatrixD.CreateFromAxisAngle(rotationAxis, angle * t));
                    waypoints[i] = planetCenter + rotatedPC * radius;
                }
                return waypoints;
            }

            public override bool Run() {

                switch (RunState) {

                    case RunStates.ASCEND:
                        // Ascend to 100m
                        if (FlightController.MaintainSurfaceAltitude(100)) {
                            RunState = RunStates.CRUISE;
                        }
                        break;

                    case RunStates.CRUISE:
                        // Cruise to job site
                        if (CurrentWaypoint == CruiseWaypoints.Length) {
                            RunState = RunStates.DESCEND;
                            break;
                        }

                        if (FlyToWaypoint(CruiseWaypoints[CurrentWaypoint])) {
                            CurrentWaypoint++;
                        }
                        break;

                    case RunStates.DESCEND:
                        // Descend to 100m
                        if (FlightController.MaintainSurfaceAltitude(10)) {
                            return true;
                        }
                        break;

                }

                return false;

            }

            private bool FlyToWaypoint(Vector3 waypoint) {
                FlightController.SetStableForwardVelocity(25);
                FlightController.AlignShipToTarget(waypoint);
                if (Vector3D.Distance(FlightController.CurrentPosition, waypoint) < 10) {
                    return true;
                }
                return false;
            }

        }
    }
}
