using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public partial class STUPointOrbitController {

                enum PointOrbitState {
                    Idle,
                    EnteringOrbit,
                    Orbiting,
                    StopOrbit
                }

                PointOrbitState CurrentState = PointOrbitState.Idle;
                STUFlightController FlightController { get; set; }
                IMyRemoteControl RemoteControl { get; set; }
                Line OrbitalAxis { get; set; }

                private double TargetRadius { get; set; }
                private double TargetAltitude { get; set; }
                public double TargetVelocity { get; set; }

                public STUPointOrbitController(STUFlightController flightController, IMyRemoteControl remoteControl) {
                    FlightController = flightController;
                    RemoteControl = remoteControl;
                }

                public bool Run(Vector3D targetPos) {
                    switch (CurrentState) {
                        case PointOrbitState.Idle:
                            TargetRadius = Vector3D.Distance(targetPos, RemoteControl.CenterOfMass);
                            TargetAltitude = FlightController._altitudeController.CurrentSeaLevelAltitude;
                            CurrentState = PointOrbitState.EnteringOrbit;
                            TargetVelocity = FindMaximumOrbitVelocity();
                            break;
                        case PointOrbitState.EnteringOrbit:
                            if (EnterOrbit(targetPos)) {
                                CurrentState = PointOrbitState.Orbiting;
                                // Lock-in the current radius for PID purposes
                                if (InGravity()) {
                                    Vector3D gravityUnitVector = Vector3D.Normalize(RemoteControl.GetNaturalGravity());
                                    OrbitalAxis = new Line(targetPos - gravityUnitVector * TargetRadius, targetPos + gravityUnitVector * TargetRadius);
                                } else {
                                    Vector3D velocityUnitVector = Vector3D.Normalize(RemoteControl.GetShipVelocities().LinearVelocity);
                                    Vector3D orbitalAxisUnitVector = Vector3D.Normalize(Vector3D.Cross(velocityUnitVector, targetPos - RemoteControl.CenterOfMass));
                                }
                            }
                            break;
                        case PointOrbitState.Orbiting:
                            ExertCentripetalForce();
                            break;
                    }
                    return false;
                }

                private bool EnterOrbit(Vector3D targetPos) {

                    double velocity = FlightController.CurrentVelocity_LocalFrame.Length();
                    Vector3D nonColinearVector;

                    // If we're in gravity, use the gravity vector as the non-colinear vector
                    nonColinearVector = InGravity() ? FlightController._velocityController.LocalGravityVector : new Vector3D(0, 0, 1);

                    if (velocity < TargetVelocity) {
                        Vector3D radiusVector = targetPos - FlightController.CurrentPosition;
                        Vector3D initialOrbitVector = Vector3D.Cross(radiusVector, nonColinearVector);
                        Vector3D kickstartThrust = Vector3D.Normalize(initialOrbitVector) * STUVelocityController.ShipMass;
                        Vector3D counterGravityForceVector = FlightController.GetAltitudeVelocityChangeForceVector(0, FlightController._altitudeController.SeaLevelAltitudeVelocity);
                        Vector3D transposedKickstartThrust = STUTransformationUtils.WorldDirectionToLocalDirection(RemoteControl, kickstartThrust);
                        Vector3D outputVector = transposedKickstartThrust + counterGravityForceVector;
                        FlightController._velocityController.ExertVectorForce_LocalFrame(outputVector, outputVector.Length());
                        return false;
                    }

                    return true;
                }


                public void ExertCentripetalForce() {

                    double altitude = FlightController._altitudeController.GetSeaLevelAltitude();
                    double mass = STUVelocityController.ShipMass;
                    double velocity = FlightController.CurrentVelocity_LocalFrame.Length();
                    double velocitySquared = velocity * velocity;
                    double radius = Vector3D.Distance(GetClosestPointOnOrbitalAxis(), RemoteControl.CenterOfMass);

                    double velocityError = TargetVelocity - velocity;
                    double altitudeError = TargetAltitude - altitude;
                    double radiusError = TargetRadius - radius;

                    double centripetalForceRequired = ((mass * velocitySquared) / TargetRadius) - 10 * radiusError;
                    Vector3D centripetalForceVector = STUTransformationUtils.WorldDirectionToLocalDirection(RemoteControl, GetUnitVectorTowardOrbitalAxis() * centripetalForceRequired);
                    Vector3D counterGravityForceVector = FlightController.GetAltitudeVelocityChangeForceVector(altitudeError, FlightController._altitudeController.SeaLevelAltitudeVelocity);

                    Vector3D outputVector = centripetalForceVector + counterGravityForceVector;

                    FlightController._velocityController.ExertVectorForce_LocalFrame(outputVector, outputVector.Length());

                }

                /// <summary>
                /// Finds the closest point on the orbital axis to the ship; https://en.wikipedia.org/wiki/Vector_projection
                /// </summary>
                /// <returns></returns>
                private Vector3D GetClosestPointOnOrbitalAxis() {
                    Vector3D b = OrbitalAxis.To - OrbitalAxis.From;
                    Vector3D a = RemoteControl.CenterOfMass - OrbitalAxis.From;
                    double t = Vector3D.Dot(a, b) / Vector3D.Dot(b, b);
                    if (t < 0) {
                        return OrbitalAxis.From;
                    } else if (t > 1) {
                        return OrbitalAxis.To;
                    } else {
                        return OrbitalAxis.From + t * b;
                    }
                }

                /// <summary>
                /// Returns a unit vector pointing from the ship to the closest point on the orbital axis
                /// </summary>
                /// <returns></returns>
                private Vector3D GetUnitVectorTowardOrbitalAxis() {
                    Vector3D closestPoint = GetClosestPointOnOrbitalAxis();
                    return Vector3D.Normalize(closestPoint - RemoteControl.CenterOfMass);
                }

                private bool InGravity() {
                    return FlightController._velocityController.LocalGravityVector != Vector3D.Zero;
                }

                private double FindMaximumOrbitVelocity() {
                    Vector3D weakestThrustVector = FlightController._velocityController.MinimumThrustVector;
                    double weakestThrust = weakestThrustVector.Length();
                    double mass = STUVelocityController.ShipMass;
                    double maximumOrbitVelocity = Math.Sqrt(weakestThrust * TargetRadius / mass);
                    return maximumOrbitVelocity;
                }

            }
        }
    }
}
