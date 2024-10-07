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

                double ALTITUDE_ERROR_TOLERANCE = 1;

                public STUAltitudeController(STUFlightController flightController, IMyRemoteControl remoteControl) {
                    FlightController = flightController;
                    RemoteControl = remoteControl;
                    CurrentSurfaceAltitude = PreviousSurfaceAltitude = GetSurfaceAltitude();
                    CurrentSeaLevelAltitude = PreviousSeaLevelAltitude = GetSeaLevelAltitude();
                    CurrentState = AltitudeState.Idle;
                }

                public bool MaintainSurfaceAltitude() {
                    switch (CurrentState) {
                        case AltitudeState.Idle:
                            return IdleSurfaceAltitude();
                        case AltitudeState.Ascending:
                            if (SetSurfaceVa(5, SurfaceAltitudeVelocity)) {
                                CurrentState = AltitudeState.Idle;
                            }
                            break;
                        case AltitudeState.Descending:
                            if (SetSurfaceVa(-5, SurfaceAltitudeVelocity)) {
                                CurrentState = AltitudeState.Idle;
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
                            if (SetSeaLevelVa(5, SeaLevelAltitudeVelocity)) {
                                CreateInfoFlightLog("Reached target sea level altitude");
                                CurrentState = AltitudeState.Idle;
                            }
                            break;
                        case AltitudeState.Descending:
                            if (SetSeaLevelVa(-5, SeaLevelAltitudeVelocity)) {
                                CreateInfoFlightLog("Reached target sea level altitude");
                                CurrentState = AltitudeState.Idle;
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
                    if (surfaceAltitudeError < ALTITUDE_ERROR_TOLERANCE) {
                        SetSurfaceVa(0, SurfaceAltitudeVelocity);
                        return true;
                    }

                    double altitude = GetSurfaceAltitude();
                    if (altitude > TargetSurfaceAltitude) {
                        CreateInfoFlightLog($"Descending to target surface altitude: {TargetSurfaceAltitude}");
                        CurrentState = AltitudeState.Descending;
                    } else if (altitude < TargetSurfaceAltitude) {
                        CreateInfoFlightLog($"Ascending to target surface altitude: {TargetSurfaceAltitude}");
                        CurrentState = AltitudeState.Ascending;
                    }
                    return false;
                }

                public bool IdleSeaLevelAltitude() {
                    double seaLevelAltitudeError = GetSeaLevelAltitudeError();
                    // if we're close enough, don't do anything
                    if (seaLevelAltitudeError < ALTITUDE_ERROR_TOLERANCE) {
                        SetSeaLevelVa(0, SeaLevelAltitudeVelocity);
                        return true;
                    }

                    double altitude = GetSeaLevelAltitude();
                    if (altitude > TargetSeaLevelAltitude) {
                        CreateInfoFlightLog($"Descending to target sea level altitude: {TargetSeaLevelAltitude}");
                        CurrentState = AltitudeState.Descending;
                    } else if (altitude < TargetSeaLevelAltitude) {
                        CreateInfoFlightLog($"Ascending to target sea level altitude: {TargetSeaLevelAltitude}");
                        CurrentState = AltitudeState.Ascending;
                    }
                    return false;
                }

                public bool SetSurfaceVa(double desiredVelocity, double altitudeVelocity) {
                    Vector3D counterGravityForceVector = FlightController.GetAltitudeVelocityChangeForceVector(desiredVelocity, altitudeVelocity);
                    // NOTE: counterGravityForceVector has already been transformed to local frame of reference
                    FlightController.VelocityController.ExertVectorForce_LocalFrame(counterGravityForceVector, counterGravityForceVector.Length());
                    return GetSurfaceAltitudeError() < ALTITUDE_ERROR_TOLERANCE;
                }

                public bool SetSeaLevelVa(double desiredVelocity, double altitudeVelocity) {
                    Vector3D counterGravityForceVector = FlightController.GetAltitudeVelocityChangeForceVector(desiredVelocity, altitudeVelocity);
                    // NOTE: counterGravityForceVector has already been transformed to local frame of reference
                    FlightController.VelocityController.ExertVectorForce_LocalFrame(counterGravityForceVector, counterGravityForceVector.Length());
                    return GetSeaLevelAltitudeError() < ALTITUDE_ERROR_TOLERANCE;
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

