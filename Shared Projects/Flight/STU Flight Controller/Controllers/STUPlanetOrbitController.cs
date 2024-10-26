
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public partial class STUPlanetOrbitController {

                enum PlanetOrbitState {
                    Initialize,
                    Idle,
                    AdjustingVelocity,
                    AdjustingAltitude,
                    Abort
                }

                double TargetVelocity;
                double TargetAltitude;
                STUGalacticMap.Planet? TargetPlanet;

                PlanetOrbitState State;

                const double VELOCITY_ERROR_TOLERANCE = 3;
                // Sets an upper-bound on how much the ship can be off-target in terms of altitude before it attempts to correct
                // The AltitudeController has a default tolerance of 1m, so it will more finely tune the altitude from there
                const double ALTITUDE_ERROR_TOLERANCE = 30;

                STUFlightController _flightController;

                public STUPlanetOrbitController(STUFlightController controller) {
                    State = PlanetOrbitState.Initialize;
                    _flightController = controller;
                }

                public bool Run() {

                    if (_flightController.RemoteControl.GetNaturalGravity().Length() == 0) {
                        CreateErrorFlightLog("ABORT -- NO GRAVITY");
                        State = PlanetOrbitState.Abort;
                    }

                    switch (State) {

                        case PlanetOrbitState.Initialize:
                            // figure out TargetPlanet, using galactic map
                            TargetPlanet = _flightController.GetPlanetOfPoint(_flightController.CurrentPosition);
                            if (!TargetPlanet.HasValue) {
                                CreateFatalFlightLog("ABORT -- NO TARGET PLANET");
                            }
                            double gravityMagnitudeAtOrbitAltitude = _flightController._velocityController.LocalGravityVector.Length();
                            double targetRadius = Vector3D.Distance(_flightController.RemoteControl.CenterOfMass, TargetPlanet.Value.Center);
                            TargetVelocity = Math.Sqrt(gravityMagnitudeAtOrbitAltitude * targetRadius);
                            TargetAltitude = _flightController._altitudeController.GetSeaLevelAltitude();

                            State = PlanetOrbitState.Idle;
                            break;

                        case PlanetOrbitState.Idle:

                            if (!WithinVelocityErrorTolerance()) {
                                CreateInfoFlightLog("Entering AdjustingVelocity");
                                _flightController.ToggleThrusters(true);
                                State = PlanetOrbitState.AdjustingVelocity;
                                break;
                            }

                            if (!WithinAltitudeErrorTolerance()) {
                                CreateInfoFlightLog("Entering AdjustingAltitude");
                                _flightController.ToggleThrusters(true);
                                State = PlanetOrbitState.AdjustingAltitude;
                                break;
                            }

                            break;

                        case PlanetOrbitState.AdjustingVelocity:
                            if (AdjustVelocity()) {
                                _flightController.ToggleThrusters(false);
                                State = PlanetOrbitState.Idle;
                            }
                            break;

                        case PlanetOrbitState.AdjustingAltitude:
                            if (AdjustAltitude()) {
                                _flightController.ToggleThrusters(false);
                                State = PlanetOrbitState.Idle;
                            }
                            break;

                        case PlanetOrbitState.Abort:
                            _flightController.RelinquishGyroControl();
                            _flightController.RelinquishThrusterControl();
                            throw new Exception("Aborting");

                    }

                    return false;

                }

                private bool AdjustVelocity() {
                    // One tick of velocity to get started
                    try {
                        if (_flightController.RemoteControl.GetShipVelocities().LinearVelocity.Length() == 0) {
                            Vector3D gravityVector = _flightController.RemoteControl.GetNaturalGravity();
                            Vector3D initialOrbitVector = Vector3D.Cross(gravityVector, new Vector3D(0, 0, 1));
                            Vector3D kickstartVelocityForce = Vector3D.Normalize(initialOrbitVector) * STUVelocityController.ShipMass;
                            _flightController._velocityController.ExertVectorForce_WorldFrame(kickstartVelocityForce, kickstartVelocityForce.Length());
                            return false;
                        }

                        Vector3D velocityVector = _flightController.RemoteControl.GetShipVelocities().LinearVelocity;
                        Vector3D velocityUnitVector = Vector3D.Normalize(velocityVector);

                        double velocityMagnitude = velocityVector.Length();
                        double velocityError = TargetVelocity - velocityMagnitude;

                        double outputForce = STUVelocityController.ShipMass * velocityError;
                        Vector3D outputForceVector = velocityUnitVector * outputForce;
                        _flightController._velocityController.ExertVectorForce_WorldFrame(outputForceVector, outputForceVector.Length());
                        return Math.Abs(velocityError) < VELOCITY_ERROR_TOLERANCE;

                    } catch (Exception e) {
                        CreateFatalFlightLog(e.ToString());
                        return false;
                    }
                }

                private bool AdjustAltitude() {
                    try {
                        return _flightController.MaintainSeaLevelAltitude(TargetAltitude, 5, -5);
                    } catch (Exception e) {
                        CreateFatalFlightLog(e.ToString());
                        return false;
                    }
                }

                private bool WithinVelocityErrorTolerance() {
                    Vector3D velocityVector = _flightController.RemoteControl.GetShipVelocities().LinearVelocity;
                    double velocityMagnitude = velocityVector.Length();
                    double velocityError = Math.Abs(TargetVelocity - velocityMagnitude);
                    return velocityError < VELOCITY_ERROR_TOLERANCE;
                }

                private bool WithinAltitudeErrorTolerance() {
                    return Math.Abs(TargetAltitude - _flightController._altitudeController.GetSeaLevelAltitude()) < ALTITUDE_ERROR_TOLERANCE;
                }

            }
        }
    }
}
