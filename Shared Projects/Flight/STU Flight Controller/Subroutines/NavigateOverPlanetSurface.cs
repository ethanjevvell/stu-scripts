using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class NavigateOverPlanetSurface : STUStateMachine {

                static class RunStates {
                    public const string INITIAL_ASCENT = "INITIAL_ASCENT";
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

                STUFlightController _flightController { get; set; }
                double _cruiseAltitude { get; set; }
                double _cruiseVelocity { get; set; }
                double _ascendVelocity { get; set; }
                double _descendVelocity { get; set; }
                Vector3D _destination { get; set; }
                STUGalacticMap.Planet? _currentPlanet { get; set; }
                bool _initialAscentComplete { get; set; }

                Vector3D _headingVector { get; set; }

                public NavigateOverPlanetSurface(STUFlightController flightController, Vector3D destination, double cruiseAltitude, double cruiseVelocity, double ascendVelocity, double descendVelocity) {
                    _flightController = flightController;
                    _cruiseAltitude = cruiseAltitude;
                    _cruiseVelocity = cruiseVelocity;
                    _ascendVelocity = ascendVelocity;
                    _descendVelocity = descendVelocity;
                    _destination = destination;
                    _currentPlanet = STUGalacticMap.GetPlanetOfPoint(_flightController.CurrentPosition);
                    _initialAscentComplete = false;

                    // If we're not on a planet, this state machine can't run
                    if (_currentPlanet == null) {
                        CreateFatalFlightLog("Cannot navigate over planet surface: current position is not on a planet");
                    }
                }

                public override string Name => "Navigate over planet surface";

                public override bool Init() {
                    RunState = RunStates.INITIAL_ASCENT;
                    _flightController.ReinstateGyroControl();
                    _flightController.ReinstateThrusterControl();
                    _flightController.GotoAndStopManeuver = new GotoAndStop(_flightController, _destination, _cruiseVelocity);
                    _flightController.ToggleThrusters(true);
                    _flightController.UpdateShipMass();
                    CreateInfoFlightLog("Init NOPS complete");
                    return true;
                }

                public override bool Closeout() {
                    if (_flightController.SetStableForwardVelocity(0)) {
                        return true;
                    }
                    return false;
                }

                public override bool Run() {
                    // Only check for final approach after initial ascent is complete
                    if (_initialAscentComplete && ApproachingDestination()) {
                        RunState = RunStates.FINAL_APPROACH;
                    }
                    // Altitude adjustment check only applies during cruise
                    else if (_initialAscentComplete &&
                            (RunState == RunStates.CRUISE || RunState == RunStates.ADJUST_VELOCITY) &&
                            (_flightController.GetCurrentSurfaceAltitude() < 0.7 * _cruiseAltitude ||
                             _flightController.GetCurrentSurfaceAltitude() > 1.3 * _cruiseAltitude)) {
                        RunState = RunStates.ADJUST_ALTITUDE;
                    }

                    switch (RunState) {
                        case RunStates.INITIAL_ASCENT:
                            if (_flightController.MaintainSurfaceAltitude(_cruiseAltitude, _ascendVelocity, _descendVelocity)) {
                                _initialAscentComplete = true;
                                RunState = RunStates.ADJUST_VELOCITY;
                                _headingVector = GetGreatCircleCruiseVector(_flightController.CurrentPosition, _destination, _currentPlanet.Value);
                            }
                            break;

                        case RunStates.ADJUST_ALTITUDE:
                            if (_flightController.MaintainSurfaceAltitude(_cruiseAltitude, _ascendVelocity, _descendVelocity)) {
                                RunState = RunStates.ADJUST_VELOCITY;
                                _headingVector = GetGreatCircleCruiseVector(_flightController.CurrentPosition, _destination, _currentPlanet.Value);
                            }
                            break;

                        case RunStates.ADJUST_VELOCITY:
                            bool stable = _flightController.SetV_WorldFrame(_headingVector, _cruiseVelocity);
                            if (stable) {
                                RunState = RunStates.CRUISE;
                            }
                            break;

                        case RunStates.CRUISE:
                            if (Math.Abs(_flightController.GetCurrentSurfaceAltitude() - _cruiseAltitude) > 5) {
                                RunState = RunStates.ADJUST_ALTITUDE;
                                break;
                            }
                            if (Math.Abs(_flightController.VelocityMagnitude - _cruiseVelocity) > 5) {
                                RunState = RunStates.ADJUST_VELOCITY;
                                _headingVector = GetGreatCircleCruiseVector(_flightController.CurrentPosition, _destination, _currentPlanet.Value);
                            }
                            break;

                        case RunStates.FINAL_APPROACH:
                            if (_flightController.GotoAndStopManeuver.ExecuteStateMachine()) {
                                return true;
                            }
                            break;
                    }

                    return false;
                }

                private bool ApproachingDestination() {
                    return Vector3D.Distance(_flightController.CurrentPosition, _destination) < _cruiseAltitude * 2;
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