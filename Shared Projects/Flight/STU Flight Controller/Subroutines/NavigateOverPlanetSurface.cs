using System;
using VRageMath;


namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class NavigateOverPlanetSurface : STUStateMachine {

                static class RunStates {
                    public const string ADJUST_VELOCITY = "ADJUST_VELOCITY";
                    public const string ADJUST_ALTITUDE = "ADJUST_ALTITUDE";
                    public const string CRUISE = "CRUISE";
                    public const string FINAL_APPROACH = "DECELERATE";
                }

                string _runState;
                string RunState {
                    get { return _runState; }
                    set {
                        CreateInfoFlightLog($"{Name} transitioning to {value}");
                        _runState = value;
                    }
                }

                STUFlightController FlightController { get; set; }
                public int CruiseAltitude { get; set; }
                public int CruiseVelocity { get; set; }
                Vector3D Destination { get; set; }
                STUGalacticMap.Planet? CurrentPlanet { get; set; }

                Vector3D _headingVector { get; set; }

                public NavigateOverPlanetSurface(STUFlightController flightController, Vector3D destination, int cruiseAltitude, int cruiseVelocity) {
                    FlightController = flightController;
                    CruiseAltitude = cruiseAltitude;
                    CruiseVelocity = cruiseVelocity;
                    Destination = destination;
                    CurrentPlanet = STUGalacticMap.GetPlanetOfPoint(FlightController.CurrentPosition);
                    // If we're not on a planet, this state machine can't run
                    if (CurrentPlanet == null) {
                        CreateFatalFlightLog("Cannot navigate over planet surface: current position is not on a planet");
                    }
                }

                public override string Name => "Navigate over planet surface";

                public override bool Init() {
                    RunState = RunStates.ADJUST_ALTITUDE;
                    FlightController.ReinstateGyroControl();
                    FlightController.ReinstateThrusterControl();
                    FlightController.GotoAndStopManeuver = new GotoAndStop(FlightController, Destination, CruiseVelocity);
                    FlightController.ToggleThrusters(true);
                    FlightController.UpdateShipMass();
                    CreateInfoFlightLog("Init NOPS complete");
                    return true;
                }

                public override bool Closeout() {
                    if (FlightController.SetStableForwardVelocity(0)) {
                        return true;
                    }
                    return false;
                }

                public override bool Run() {

                    if (ApproachingDestination()) {
                        RunState = RunStates.FINAL_APPROACH;
                    } else if (FlightController.GetCurrentSurfaceAltitude() < 0.7 * CruiseAltitude || FlightController.GetCurrentSurfaceAltitude() > 1.3 * CruiseAltitude) {
                        RunState = RunStates.ADJUST_ALTITUDE;
                    }

                    switch (RunState) {

                        case RunStates.ADJUST_ALTITUDE:
                            if (FlightController.MaintainSurfaceAltitude(CruiseAltitude)) {
                                RunState = RunStates.ADJUST_VELOCITY;
                                _headingVector = GetGreatCircleCruiseVector(FlightController.CurrentPosition, Destination, CurrentPlanet.Value);
                            }
                            break;

                        case RunStates.ADJUST_VELOCITY:
                            bool stable = FlightController.SetV_WorldFrame(_headingVector, CruiseVelocity);
                            if (stable) {
                                RunState = RunStates.CRUISE;
                            }
                            break;

                        case RunStates.CRUISE:
                            if (Math.Abs(FlightController.GetCurrentSurfaceAltitude() - CruiseAltitude) > 5) {
                                RunState = RunStates.ADJUST_ALTITUDE;
                                break;
                            }
                            if (Math.Abs(FlightController.VelocityMagnitude - CruiseVelocity) > 5) {
                                RunState = RunStates.ADJUST_VELOCITY;
                                _headingVector = GetGreatCircleCruiseVector(FlightController.CurrentPosition, Destination, CurrentPlanet.Value);
                            }
                            break;

                        case RunStates.FINAL_APPROACH:
                            if (FlightController.GotoAndStopManeuver.ExecuteStateMachine()) {
                                return true;
                            }
                            break;

                    }

                    return false;

                }

                private bool ApproachingDestination() {
                    return Vector3D.Distance(FlightController.CurrentPosition, Destination) < CruiseAltitude * 2;
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
}
