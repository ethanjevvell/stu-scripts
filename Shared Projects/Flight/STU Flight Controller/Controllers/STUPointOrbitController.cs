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
                public double KickstartVelocity { get; set; }

                public STUPointOrbitController(STUFlightController flightController, IMyRemoteControl remoteControl) {
                    FlightController = flightController;
                    RemoteControl = remoteControl;
                    KickstartVelocity = 2.5;
                }

                public bool Run(Vector3D targetPos) {
                    switch (CurrentState) {
                        case PointOrbitState.Idle:
                            // do nothing
                            CurrentState = PointOrbitState.EnteringOrbit;
                            break;
                        case PointOrbitState.EnteringOrbit:
                            if (EnterOrbit(targetPos)) {
                                CurrentState = PointOrbitState.Orbiting;
                                // Lock-in the current radius for PID purposes
                                TargetRadius = Vector3D.Distance(targetPos, RemoteControl.CenterOfMass);
                                TargetAltitude = FlightController.AltitudeController.CurrentSeaLevelAltitude;
                                if (InGravity()) {
                                    Vector3D gravityUnitVector = Vector3D.Normalize(RemoteControl.GetNaturalGravity());
                                    OrbitalAxis = new Line(targetPos - gravityUnitVector * TargetRadius, targetPos + gravityUnitVector * TargetRadius);
                                } else {
                                    Vector3D velocityUnitVector = Vector3D.Normalize(RemoteControl.GetShipVelocities().LinearVelocity);
                                    Vector3D orbitalAxisUnitVector = Vector3D.Normalize(Vector3D.Cross(velocityUnitVector, targetPos - RemoteControl.CenterOfMass));
                                    OrbitalAxis = new Line(targetPos - orbitalAxisUnitVector * TargetRadius, targetPos + orbitalAxisUnitVector * TargetRadius);
                                }
                                //LIGMA.CreateOkBroadcast($"Start: {OrbitalAxis.From}");
                                //LIGMA.CreateOkBroadcast($"End: {OrbitalAxis.To}");
                            }
                            break;
                        case PointOrbitState.Orbiting:
                            ExertCentripetalForce();
                            break;
                    }
                    return false;
                }

                private bool EnterOrbit(Vector3D targetPos) {

                    double velocity = FlightController.CurrentVelocity.Length();
                    Vector3D nonColinearVector;

                    // If we're in gravity, use the gravity vector as the non-colinear vector
                    nonColinearVector = InGravity() ? FlightController.VelocityController.LocalGravityVector : new Vector3D(0, 0, 1);

                    if (velocity < KickstartVelocity) {
                        Vector3D radiusVector = targetPos - FlightController.CurrentPosition;
                        Vector3D initialOrbitVector = Vector3D.Cross(radiusVector, nonColinearVector);
                        Vector3D kickstartThrust = Vector3D.Normalize(initialOrbitVector) * STUVelocityController.ShipMass;
                        Vector3D counterGravityForceVector = FlightController.GetCounterGravityForceVector(0, FlightController.AltitudeController.SeaLevelAltitudeVelocity);
                        FlightController.ExertVectorForce(Vector3D.TransformNormal(kickstartThrust, MatrixD.Transpose(FlightController.CurrentWorldMatrix)) * new Vector3D(1, 1, -1) + counterGravityForceVector);
                        return false;
                    }

                    return true;
                }


                public void ExertCentripetalForce() {
                    double mass = STUVelocityController.ShipMass;
                    double velocity = FlightController.CurrentVelocity.Length();
                    double velocitySquared = velocity * velocity;
                    double radius = Vector3D.Distance(GetClosestPointOnOrbitalAxis(), RemoteControl.CenterOfMass);
                    double radiusError = radius - TargetRadius;
                    double centripetalForceRequired = ((mass * velocitySquared) / TargetRadius) + 10 * radiusError;
                    Vector3D centripetalForceVector = GetUnitVectorTowardOrbitalAxis() * centripetalForceRequired;
                    Vector3D counterGravityForceVector = FlightController.GetCounterGravityForceVector(0, FlightController.AltitudeController.SeaLevelAltitudeVelocity);
                    FlightController.ExertVectorForce(Vector3D.TransformNormal(centripetalForceVector, MatrixD.Transpose(FlightController.CurrentWorldMatrix)) * new Vector3D(1, 1, -1) + counterGravityForceVector);
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
                        //LIGMA.CreateWarningBroadcast($"Below OA");
                        return OrbitalAxis.From;
                    } else if (t > 1) {
                        //LIGMA.CreateWarningBroadcast($"Above OA");
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
                    return FlightController.VelocityController.LocalGravityVector != Vector3D.Zero;
                }

            }
        }
    }
}
