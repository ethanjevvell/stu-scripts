﻿using Sandbox.ModAPI.Ingame;
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
                                    Vector3D gravityUnitVector = Vector3D.Normalize(FlightController.VelocityController.LocalGravityVector);
                                    OrbitalAxis = new Line(targetPos - gravityUnitVector * TargetRadius, targetPos + gravityUnitVector * TargetRadius);
                                } else {
                                    Vector3D velocityUnitVector = Vector3D.Normalize(RemoteControl.GetShipVelocities().LinearVelocity);
                                    Vector3D orbitalAxisUnitVector = Vector3D.Normalize(Vector3D.Cross(velocityUnitVector, targetPos - RemoteControl.CenterOfMass));
                                    OrbitalAxis = new Line(targetPos - orbitalAxisUnitVector * TargetRadius, targetPos + orbitalAxisUnitVector * TargetRadius);
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

                    double velocity = FlightController.CurrentVelocity.Length();
                    Vector3D nonColinearVector;

                    // if we have a gravity vector, use that as the non-colinear vector
                    // This ensure the orbit path is parallel with the surface of the planet
                    if (InGravity()) {
                        nonColinearVector = FlightController.VelocityController.LocalGravityVector;
                    } else {
                        nonColinearVector = new Vector3D(0, 0, 1);
                    }

                    if (velocity < KickstartVelocity) {
                        Vector3D radiusVector = targetPos - FlightController.CurrentPosition;
                        Vector3D initialOrbitVector = Vector3D.Cross(radiusVector, nonColinearVector);
                        Vector3D kickstartThrust = Vector3D.Normalize(initialOrbitVector) * STUVelocityController.ShipMass;
                        Vector3D counterGravityForceVector = FlightController.GetCounterGravityForceVector(0, FlightController.AltitudeController.SeaLevelAltitudeVelocity);
                        FlightController.ExertVectorForce(kickstartThrust + counterGravityForceVector);
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
                    // create a vector that points from the ship to the target
                    // if velocity is really close to zero, we need to kickstart an orbit
                    double centripetalForceRequired = ((mass * velocitySquared) / radius) + 100 * radiusError;
                    Vector3D centripetalForceVector = GetUnitVectorTowardOrbitalAxis() * centripetalForceRequired;
                    //LIGMA.CreateOkBroadcast($"F_c_adj = {centripetalForce}");
                    //LIGMA.CreateOkBroadcast($"F_c_raw = {(mass * velocitySquared) / radius}");
                    //LIGMA.CreateOkBroadcast($"V_c = {FlightController.CurrentVelocity.Length()}");
                    //LIGMA.CreateOkBroadcast($"r = {radius}");
                    //LIGMA.CreateOkBroadcast($"r_e = {radiusError}");
                    //LIGMA.CreateOkBroadcast($"r_t = {TargetRadius}");
                    Vector3D counterGravityForceVector = FlightController.GetCounterGravityForceVector(0, FlightController.AltitudeController.SeaLevelAltitudeVelocity);
                    FlightController.ExertVectorForce(Vector3D.TransformNormal(centripetalForceVector, MatrixD.Transpose(FlightController.CurrentWorldMatrix)) * new Vector3D(1, 1, -1) + counterGravityForceVector);
                }

                private Vector3D GetClosestPointOnOrbitalAxis() {
                    Vector3D shipToAxisStart = OrbitalAxis.From - RemoteControl.CenterOfMass;
                    double projectionLength = Vector3D.Dot(shipToAxisStart, OrbitalAxis.Direction);
                    return OrbitalAxis.From + projectionLength * Vector3D.Normalize(OrbitalAxis.Direction);
                }

                private Vector3D GetUnitVectorTowardOrbitalAxis() {
                    Vector3D closestPointOnOrbitalAxis = GetClosestPointOnOrbitalAxis();
                    return Vector3D.Normalize(closestPointOnOrbitalAxis - RemoteControl.CenterOfMass);
                }

                private bool InGravity() {
                    return FlightController.VelocityController.LocalGravityVector != Vector3D.Zero;
                }

            }
        }
    }
}
