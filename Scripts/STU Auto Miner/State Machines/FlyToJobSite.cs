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
            List<Vector3> CruiseWaypoints { get; set; }
            int CurrentWaypoint { get; set; }
            public Vector3 JobSite { get; set; }
            public int CruiseAltitude { get; set; }

            public FlyToJobSite(STUFlightController fc, IMyShipConnector connector, List<IMyGasTank> hydrogenTanks, List<IMyBatteryBlock> batteries, Vector3 jobSite, int cruiseAltitude) {
                FlightController = fc;
                Connector = connector;
                HydrogenTanks = hydrogenTanks;
                Batteries = batteries;
                RunState = RunStates.ASCEND;
                JobSite = jobSite;
                CruiseAltitude = cruiseAltitude;
            }

            public override string Name => "Fly To Job Site";

            public override bool Init() {
                Connector.Disconnect();
                HydrogenTanks.ForEach(tank => tank.Stockpile = true);
                Batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Auto);
                if (JobSite == null) {
                    CreateFatalErrorBroadcast("No job site loaded into memory; did you forget to set this.JobSite?");
                }
                CruiseWaypoints = CalculateCruiseWaypoints(JobSite, 10, CruiseAltitude);
                if (CruiseWaypoints == null || CruiseWaypoints.Count == 0) {
                    CreateFatalErrorBroadcast($"Failed to calculate cruise waypoints; cruise waypoints length: {CruiseWaypoints.Count}");
                }
                for (int i = 0; i < CruiseWaypoints.Count; i++) {
                    CreateInfoBroadcast($"Waypoint {i}: {CruiseWaypoints[i]}");
                }
                return true;
            }

            public override bool Closeout() {
                // TODO
                return true;
            }

            List<Vector3> CalculateCruiseWaypoints(Vector3 jobSite, int n, int cruiseAltitude) {
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
                rotationAxis.Normalize();

                double angle = Math.Acos(Vector3.Dot(PC, PJ));
                double cruiseRadius = radius1 + cruiseAltitude;

                List<Vector3> waypoints = new List<Vector3>();
                for (int i = 0; i < n; i++) {
                    double t = (double)i / (n - 1); // Ensures inclusion of endpoint
                    Vector3D rotatedPC = Vector3D.Transform(PC, MatrixD.CreateFromAxisAngle(rotationAxis, angle * t));
                    waypoints.Add(planetCenter + rotatedPC * cruiseRadius);
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
                        if (CurrentWaypoint == CruiseWaypoints.Count) {
                            RunState = RunStates.DESCEND;
                            CreateWarningBroadcast("Reached final waypoint; descending to job site");
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
                FlightController.SetStableForwardVelocity(5);
                FlightController.AlignShipToTarget(waypoint);
                CreateInfoBroadcast($"{Vector3D.Distance(FlightController.CurrentPosition, waypoint)}");
                if (Vector3D.Distance(FlightController.CurrentPosition, waypoint) < 10) {
                    return true;
                }
                return false;
            }

        }
    }
}
