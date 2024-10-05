
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

                STUFlightController FlightController;

                public STUPlanetOrbitController(STUFlightController controller) {
                    State = PlanetOrbitState.Initialize;
                    FlightController = controller;
                }

                public bool Run() {

                    if (FlightController.RemoteControl.GetNaturalGravity().Length() == 0) {
                        CreateErrorFlightLog("ABORT -- NO GRAVITY");
                        State = PlanetOrbitState.Abort;
                    }

                    switch (State) {

                        case PlanetOrbitState.Initialize:
                            // figure out TargetPlanet, using galactic map
                            TargetPlanet = FlightController.GetPlanetOfPoint(FlightController.CurrentPosition);
                            if (!TargetPlanet.HasValue) {
                                CreateFatalFlightLog("ABORT -- NO TARGET PLANET");
                            }
                            double gravityMagnitudeAtOrbitAltitude = FlightController.VelocityController.LocalGravityVector.Length();
                            double targetRadius = Vector3D.Distance(FlightController.RemoteControl.CenterOfMass, TargetPlanet.Value.Center);
                            TargetVelocity = Math.Sqrt(gravityMagnitudeAtOrbitAltitude * targetRadius);
                            TargetAltitude = FlightController.AltitudeController.GetSeaLevelAltitude();

                            State = PlanetOrbitState.Idle;
                            break;

                        case PlanetOrbitState.Idle:

                            if (!WithinVelocityErrorTolerance()) {
                                CreateInfoFlightLog("Entering AdjustingVelocity");
                                FlightController.ToggleThrusters(true);
                                State = PlanetOrbitState.AdjustingVelocity;
                                break;
                            }

                            if (!WithinAltitudeErrorTolerance()) {
                                CreateInfoFlightLog("Entering AdjustingAltitude");
                                FlightController.ToggleThrusters(true);
                                State = PlanetOrbitState.AdjustingAltitude;
                                break;
                            }

                            break;

                        case PlanetOrbitState.AdjustingVelocity:
                            if (AdjustVelocity()) {
                                FlightController.ToggleThrusters(false);
                                State = PlanetOrbitState.Idle;
                            }
                            break;

                        case PlanetOrbitState.AdjustingAltitude:
                            if (AdjustAltitude()) {
                                FlightController.ToggleThrusters(false);
                                State = PlanetOrbitState.Idle;
                            }
                            break;

                        case PlanetOrbitState.Abort:
                            FlightController.RelinquishGyroControl();
                            FlightController.RelinquishThrusterControl();
                            throw new Exception("Aborting");

                    }

                    return false;

                }

                private bool AdjustVelocity() {
                    // One tick of velocity to get started
                    try {
                        if (FlightController.RemoteControl.GetShipVelocities().LinearVelocity.Length() == 0) {
                            Vector3D gravityVector = FlightController.RemoteControl.GetNaturalGravity();
                            Vector3D initialOrbitVector = Vector3D.Cross(gravityVector, new Vector3D(0, 0, 1));
                            Vector3D kickstartVelocityForce = Vector3D.Normalize(initialOrbitVector) * STUVelocityController.ShipMass;
                            FlightController.VelocityController.Accelerate_WorldFrame(kickstartVelocityForce, kickstartVelocityForce.Length());
                            return false;
                        }

                        Vector3D velocityVector = FlightController.RemoteControl.GetShipVelocities().LinearVelocity;
                        Vector3D velocityUnitVector = Vector3D.Normalize(velocityVector);

                        double velocityMagnitude = velocityVector.Length();
                        double velocityError = TargetVelocity - velocityMagnitude;

                        double outputForce = STUVelocityController.ShipMass * velocityError;
                        Vector3D outputForceVector = velocityUnitVector * outputForce;
                        FlightController.VelocityController.Accelerate_WorldFrame(outputForceVector, outputForceVector.Length());
                        return Math.Abs(velocityError) < VELOCITY_ERROR_TOLERANCE;

                    } catch (Exception e) {
                        CreateFatalFlightLog(e.ToString());
                        return false;
                    }
                }

                private bool AdjustAltitude() {
                    try {
                        return FlightController.MaintainSeaLevelAltitude(TargetAltitude);
                    } catch (Exception e) {
                        CreateFatalFlightLog(e.ToString());
                        return false;
                    }
                }

                private bool WithinVelocityErrorTolerance() {
                    Vector3D velocityVector = FlightController.RemoteControl.GetShipVelocities().LinearVelocity;
                    double velocityMagnitude = velocityVector.Length();
                    double velocityError = Math.Abs(TargetVelocity - velocityMagnitude);
                    return velocityError < VELOCITY_ERROR_TOLERANCE;
                }

                private bool WithinAltitudeErrorTolerance() {
                    return Math.Abs(TargetAltitude - FlightController.AltitudeController.GetSeaLevelAltitude()) < ALTITUDE_ERROR_TOLERANCE;
                }

            }
        }
    }
}
