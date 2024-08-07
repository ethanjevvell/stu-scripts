using Sandbox.ModAPI.Ingame;
using System;

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

                public bool SetVa(double desiredVelocity) {
                    // Extract the components of the local gravity vector
                    double Gx = VelocityController.LocalGravityVector.X;
                    double Gy = VelocityController.LocalGravityVector.Y;
                    double Gz = -VelocityController.LocalGravityVector.Z;

                    // Calculate the magnitude of the gravitational force
                    double gravityForceMagnitude = VelocityController.LocalGravityVector.Length();

                    // Total mass of the ship
                    double mass = STUVelocityController.ShipMass;

                    // Total force needed: F = ma for gravity + ma for additional acceleration
                    double totalForceNeeded = mass * (gravityForceMagnitude + desiredVelocity - AltitudeVelocity);

                    // Normalize the gravity vector to get the direction
                    double unitGx = Gx / gravityForceMagnitude;
                    double unitGy = Gy / gravityForceMagnitude;
                    double unitGz = Gz / gravityForceMagnitude;

                    // Calculate the force vector needed (opposite to gravity and scaled by totalForceNeeded)
                    double Fx = -unitGx * totalForceNeeded;
                    double Fy = -unitGy * totalForceNeeded;
                    double Fz = -unitGz * totalForceNeeded;

                    // Set the force components on the velocity controller
                    VelocityController.SetFx(Fx);
                    VelocityController.SetFy(Fy);
                    VelocityController.SetFz(Fz);
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

