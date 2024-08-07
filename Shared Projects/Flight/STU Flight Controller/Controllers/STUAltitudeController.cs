using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            public class STUAltitudeController {

                enum AltitudeState {
                    Idle,
                    Ascending,
                    Descending,
                    StopAltitudeChange
                }

                AltitudeState CurrentState = AltitudeState.Idle;
                STUVelocityController VelocityController { get; set; }
                IMyRemoteControl RemoteControl { get; set; }

                public double TargetAltitude { get; set; }
                public double CurrentAltitude { get; set; }
                public double PreviousAltitude { get; set; }
                public double AltitudeVelocity { get; set; }

                public STUAltitudeController(STUVelocityController velocityController, IMyRemoteControl remoteControl) {
                    VelocityController = velocityController;
                    RemoteControl = remoteControl;
                    CurrentAltitude = PreviousAltitude = GetAltitude();
                }

                public bool Run() {
                    switch (CurrentState) {
                        case AltitudeState.Idle:
                            return MaintainAltitude();
                        case AltitudeState.Ascending:
                            if (SetVa(5)) {
                                CurrentState = AltitudeState.Idle;
                            }
                            break;
                        case AltitudeState.Descending:
                            if (SetVa(-5)) {
                                CurrentState = AltitudeState.Idle;
                            }
                            break;
                    }
                    return false;
                }

                public void UpdateState() {
                    PreviousAltitude = CurrentAltitude;
                    CurrentAltitude = GetAltitude();
                    AltitudeVelocity = GetAltitudeVelocity();
                }

                private double GetAltitudeVelocity() {
                    return (CurrentAltitude - PreviousAltitude) / (1.0 / 6.0);
                }

                public bool MaintainAltitude() {
                    SetVa(0);
                    double elevationError = GetAltitudeError();
                    // if we're close enough, don't do anything
                    if (elevationError < 10) {
                        return true;
                    }

                    if (GetAltitude() > TargetAltitude) {
                        CurrentState = AltitudeState.Descending;
                    } else {
                        CurrentState = AltitudeState.Ascending;
                    }
                    return false;
                }

                public void ExertVectorForce(Vector3D forceVector) {
                    VelocityController.SetFx(forceVector.X);
                    VelocityController.SetFy(forceVector.Y);
                    VelocityController.SetFz(forceVector.Z);
                }

                public bool SetVa(double desiredVelocity) {

                    Vector3D localGravityVector = VelocityController.LocalGravityVector;

                    // Calculate the magnitude of the gravitational force
                    double gravityForceMagnitude = localGravityVector.Length();

                    // Total mass of the ship
                    double mass = STUVelocityController.ShipMass;

                    // Total force needed: F = ma; a acts as basic proportional controlller here
                    double totalForceNeeded = mass * (gravityForceMagnitude + desiredVelocity - AltitudeVelocity);

                    // Normalize the gravity vector to get the direction
                    Vector3D unitGravityVector = localGravityVector / gravityForceMagnitude;

                    // Calculate the force vector needed (opposite to gravity and scaled by totalForceNeeded)
                    Vector3D outputForce = -unitGravityVector * totalForceNeeded;

                    // Set the force components on the velocity controller
                    ExertVectorForce(outputForce);

                    return GetAltitudeError() < 10;
                }

                private double GetAltitudeError() {
                    return Math.Abs(GetAltitude() - TargetAltitude);
                }

                public double GetAltitude() {
                    double elevation;
                    if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation)) {
                        return elevation;
                    }
                    return 1;
                }

            }

        }
    }
}

