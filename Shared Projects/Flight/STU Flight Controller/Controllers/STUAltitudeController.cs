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
                STUFlightController FlightController { get; set; }
                IMyRemoteControl RemoteControl { get; set; }

                public double TargetSeaLevelAltitude { get; set; }
                public double CurrentSeaLevelAltitude { get; set; }
                public double PreviousSeaLevelAltitude { get; set; }
                public double SeaLevelAltitudeVelocity { get; set; }

                public double TargetSurfaceAltitude { get; set; }
                public double CurrentSurfaceAltitude { get; set; }
                public double PreviousSurfaceAltitude { get; set; }
                public double SurfaceAltitudeVelocity { get; set; }

                public STUAltitudeController(STUFlightController flightController, STUVelocityController velocityController, IMyRemoteControl remoteControl) {
                    FlightController = flightController;
                    VelocityController = velocityController;
                    RemoteControl = remoteControl;
                    CurrentSurfaceAltitude = PreviousSurfaceAltitude = GetSurfaceAltitude();
                    CurrentSeaLevelAltitude = PreviousSeaLevelAltitude = GetSeaLevelAltitude();
                }

                public bool MaintainSurfaceAltitude() {
                    switch (CurrentState) {
                        case AltitudeState.Idle:
                            return IdleSurfaceAltitude();
                        case AltitudeState.Ascending:
                            if (SetVa(5, SurfaceAltitudeVelocity)) {
                                CurrentState = AltitudeState.Idle;
                                SetVa(0, SurfaceAltitudeVelocity);
                            }
                            break;
                        case AltitudeState.Descending:
                            if (SetVa(-5, SurfaceAltitudeVelocity)) {
                                CurrentState = AltitudeState.Idle;
                                SetVa(0, SurfaceAltitudeVelocity);
                            }
                            break;
                    }
                    return false;
                }

                public bool MaintainSeaLevelAltitude() {
                    switch (CurrentState) {
                        case AltitudeState.Idle:
                            return IdleSeaLevelAltitude();
                        case AltitudeState.Ascending:
                            if (SetVa(5, SeaLevelAltitudeVelocity)) {
                                CurrentState = AltitudeState.Idle;
                                SetVa(0, SeaLevelAltitudeVelocity);
                            }
                            break;
                        case AltitudeState.Descending:
                            if (SetVa(-5, SeaLevelAltitudeVelocity)) {
                                CurrentState = AltitudeState.Idle;
                                SetVa(0, SeaLevelAltitudeVelocity);
                            }
                            break;
                    }
                    return false;
                }

                public void UpdateState() {
                    PreviousSurfaceAltitude = CurrentSurfaceAltitude;
                    CurrentSurfaceAltitude = GetSurfaceAltitude();
                    SurfaceAltitudeVelocity = GetSurfaceAltitudeVelocity();

                    PreviousSeaLevelAltitude = CurrentSeaLevelAltitude;
                    CurrentSeaLevelAltitude = GetSeaLevelAltitude();
                    SeaLevelAltitudeVelocity = GetSeaLevelAltitudeVelocity();
                }

                private double GetSurfaceAltitudeVelocity() {
                    return (CurrentSurfaceAltitude - PreviousSurfaceAltitude) / (1.0 / 6.0);
                }

                private double GetSeaLevelAltitudeVelocity() {
                    return (CurrentSeaLevelAltitude - PreviousSeaLevelAltitude) / (1.0 / 6.0);
                }

                public bool IdleSurfaceAltitude() {
                    double surfaceAltitudeError = GetSurfaceAltitudeError();
                    // if we're close enough, don't do anything
                    if (surfaceAltitudeError < 10) {
                        return true;
                    }

                    double altitude = GetSurfaceAltitude();
                    if (altitude > TargetSurfaceAltitude) {
                        CurrentState = AltitudeState.Descending;
                    } else if (altitude < TargetSurfaceAltitude) {
                        CurrentState = AltitudeState.Ascending;
                    }
                    return false;
                }

                public bool IdleSeaLevelAltitude() {
                    double seaLevelAltitude = GetSeaLevelAltitudeError();
                    // if we're close enough, don't do anything
                    if (seaLevelAltitude < 10) {
                        return true;
                    }

                    double altitude = GetSeaLevelAltitude();
                    if (altitude > TargetSurfaceAltitude) {
                        CurrentState = AltitudeState.Descending;
                    } else if (altitude < TargetSurfaceAltitude) {
                        CurrentState = AltitudeState.Ascending;
                    }
                    return false;
                }

                public bool SetVa(double desiredVelocity, double altitudeVelocity) {
                    // Set the force components on the velocity controller
                    Vector3D counterGravityForceVector = FlightController.GetCounterGravityForceVector(desiredVelocity, altitudeVelocity);
                    FlightController.ExertVectorForce(counterGravityForceVector);
                    return GetSurfaceAltitudeError() < 10;
                }

                private double GetSurfaceAltitudeError() {
                    return Math.Abs(GetSurfaceAltitude() - TargetSurfaceAltitude);
                }

                private double GetSeaLevelAltitudeError() {
                    return Math.Abs(GetSeaLevelAltitude() - TargetSeaLevelAltitude);
                }

                public double GetSurfaceAltitude() {
                    double elevation;
                    if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation)) {
                        return elevation;
                    }
                    return 0;
                }

                public double GetSeaLevelAltitude() {
                    double elevation;
                    if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out elevation)) {
                        return elevation;
                    }
                    return 0;
                }

            }

        }
    }
}

