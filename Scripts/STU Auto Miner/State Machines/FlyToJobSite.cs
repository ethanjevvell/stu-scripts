using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public partial class FlyToJobSite : STUStateMachine {

            public enum RunStates {
                ASCEND,
                ORIENT,
                ADJUST_VELOCITY,
                ADJUST_ALTITUDE,
                CRUISE,
                DECELERATE,
            }

            STUFlightController FlightController { get; set; }
            IMyShipConnector Connector { get; set; }
            List<IMyGasTank> HydrogenTanks { get; set; }
            List<IMyBatteryBlock> Batteries { get; set; }
            RunStates RunState { get; set; }
            public Vector3 JobSite { get; set; }
            public int CruiseAltitude { get; set; }
            public int CruiseVelocity { get; set; }
            Vector3D CruisePhaseDestination { get; set; }
            STUGalacticMap.Planet? CurrentPlanet { get; set; }

            public FlyToJobSite(STUFlightController fc, IMyShipConnector connector, List<IMyGasTank> hydrogenTanks, List<IMyBatteryBlock> batteries, Vector3 jobSite, PlaneD jobPlane, int cruiseAltitude, int cruiseVelocity) {
                FlightController = fc;
                Connector = connector;
                HydrogenTanks = hydrogenTanks;
                Batteries = batteries;
                RunState = RunStates.ASCEND;
                JobSite = jobSite;
                CruiseAltitude = cruiseAltitude;
                CruiseVelocity = cruiseVelocity;
            }

            public override string Name => "Fly To Job Site";

            public override bool Init() {
                Connector.Disconnect();
                HydrogenTanks.ForEach(tank => tank.Stockpile = false);
                Batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Auto);
                CurrentPlanet = STUGalacticMap.GetPlanetOfPoint(FlightController.CurrentPosition);

                // Calculate the cruise phase destination
                Vector3D jobSiteVector = JobSite - CurrentPlanet.Value.Center;
                jobSiteVector.Normalize();
                Vector3D cruiseDestination = CurrentPlanet.Value.Center + jobSiteVector * (Vector3D.Distance(CurrentPlanet.Value.Center, JobSite) + CruiseAltitude);
                CruisePhaseDestination = cruiseDestination;
                FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, CruisePhaseDestination, CruiseVelocity);
                return true;
            }

            public override bool Closeout() {
                if (FlightController.SetStableForwardVelocity(0)) {
                    return true;
                }
                return false;
            }

            public override bool Run() {


                switch (RunState) {

                    case RunStates.ASCEND:
                        // Ascend to 100m
                        if (FlightController.MaintainSurfaceAltitude(CruiseAltitude)) {
                            RunState = RunStates.ORIENT;
                        }
                        break;

                    case RunStates.ORIENT:
                        if (ApproachingDestination()) {
                            RunState = RunStates.DECELERATE;
                            break;
                        }
                        Vector3D headingVector = GetGreatCircleCruiseVector(FlightController.CurrentPosition, CruisePhaseDestination, CurrentPlanet.Value);
                        bool aligned = FlightController.AlignShipToTarget(headingVector);
                        bool stable = FlightController.SetStableForwardVelocity(0);
                        if (aligned && stable) {
                            RunState = RunStates.ADJUST_VELOCITY;
                        }
                        break;

                    case RunStates.ADJUST_VELOCITY:
                        if (ApproachingDestination()) {
                            RunState = RunStates.DECELERATE;
                            break;
                        }
                        if (FlightController.SetStableForwardVelocity(CruiseVelocity)) {
                            RunState = RunStates.CRUISE;
                        }
                        break;

                    case RunStates.ADJUST_ALTITUDE:
                        if (ApproachingDestination()) {
                            RunState = RunStates.DECELERATE;
                            break;
                        }
                        if (FlightController.MaintainSurfaceAltitude(CruiseAltitude)) {
                            RunState = RunStates.CRUISE;
                        }
                        break;

                    case RunStates.CRUISE:
                        if (ApproachingDestination()) {
                            RunState = RunStates.DECELERATE;
                            break;
                        }

                        if (FlightController.VelocityMagnitude < 0.9 * CruiseVelocity
                            || FlightController.VelocityMagnitude > 1.1 * CruiseVelocity) {
                            RunState = RunStates.ADJUST_VELOCITY;
                            break;
                        }

                        if (Math.Abs(FlightController.GetCurrentSurfaceAltitude() - CruiseAltitude) < 5) {
                            RunState = RunStates.ADJUST_ALTITUDE;
                            break;
                        }

                        break;

                    case RunStates.DECELERATE:
                        if (FlightController.GotoAndStopManeuver.ExecuteStateMachine()) {
                            CreateInfoBroadcast("Finished job site flight state machine");
                            return true;
                        }
                        break;

                }

                return false;

            }

            private bool ApproachingDestination() {
                return Vector3D.Distance(FlightController.CurrentPosition, CruisePhaseDestination) < CruiseVelocity * 10;
            }

            private Vector3D GetGreatCircleCruiseVector(Vector3D currentPos, Vector3D targetPos, STUGalacticMap.Planet planet) {
                Vector3D PC = currentPos - planet.Center; // Vector from planet center to current position
                Vector3D PT = targetPos - planet.Center;  // Vector from planet center to target position

                PC.Normalize(); // Normalize to unit vector
                PT.Normalize(); // Normalize to unit vector

                // Compute the normal vector to the plane defined by PC and PT (great circle plane)
                Vector3D greatCircleNormal = Vector3D.Cross(PC, PT);

                // Compute the heading vector that is tangent to the sphere at currentPos
                Vector3D headingVector = Vector3D.Cross(greatCircleNormal, PC);

                headingVector.Normalize(); // Normalize the heading vector

                // Choose a reasonable distance ahead along the heading vector
                double distanceAhead = planet.Radius * 0.1; // Adjust the scalar as needed (e.g., 10% of planet's radius)

                // Calculate the target point in space ahead along the heading direction
                Vector3D targetPoint = currentPos + headingVector * distanceAhead;

                return targetPoint; // Return the point in space for AlignShipToTarget
            }
        }
    }
}
