using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            public class STUAltitudeController {

                static class AltitudeState {
                    public const string IDLE = "IDLE";
                    public const string ASCENDING = "ASCENDING";
                    public const string DESCENDING = "DESCENDING";
                    public const string STOP_ALTITUDE_CHANGE = "STOP_ALTITUDE_CHANGE";
                }

                STUFlightController FlightController { get; set; }
                IMyRemoteControl RemoteControl { get; set; }

                public double CurrentSeaLevelAltitude { get; private set; }
                public double SeaLevelAltitudeVelocity { get; private set; }
                double _previousSeaLevelAltitude { get; set; }

                public double CurrentSurfaceAltitude { get; private set; }
                public double SurfaceAltitudeVelocity { get; private set; }
                double _previousSurfaceAltitude { get; set; }

                double ALTITUDE_ERROR_TOLERANCE = 1;
                double ASCEND_VELOCITY = 10;
                double DESCEND_VELOCITY = -5;

                public STUAltitudeController(STUFlightController flightController, IMyRemoteControl remoteControl) {
                    FlightController = flightController;
                    RemoteControl = remoteControl;
                    CurrentSurfaceAltitude = _previousSurfaceAltitude = GetSurfaceAltitude();
                    CurrentSeaLevelAltitude = _previousSeaLevelAltitude = GetSeaLevelAltitude();
                }

                public bool MaintainSurfaceAltitude(double targetAltitude) {
                    if (IdleSurfaceAltitude(targetAltitude)) {
                        return true;
                    }
                    if (GetSurfaceAltitude() < targetAltitude) {
                        if (SetSurfaceVa(ASCEND_VELOCITY, SurfaceAltitudeVelocity, targetAltitude)) {
                            return true;
                        }
                        return false;
                    }
                    return SetSurfaceVa(DESCEND_VELOCITY, SurfaceAltitudeVelocity, targetAltitude);
                }

                public bool MaintainSeaLevelAltitude(double targetAltitude) {
                    if (IdleSeaLevelAltitude(targetAltitude)) {
                        return true;
                    }
                    if (GetSeaLevelAltitude() < targetAltitude) {
                        if (SetSeaLevelVa(ASCEND_VELOCITY, SeaLevelAltitudeVelocity, targetAltitude)) {
                            return true;
                        }
                        return false;
                    }
                    return SetSeaLevelVa(DESCEND_VELOCITY, SeaLevelAltitudeVelocity, targetAltitude);
                }

                public void UpdateState() {
                    _previousSurfaceAltitude = CurrentSurfaceAltitude;
                    CurrentSurfaceAltitude = GetSurfaceAltitude();
                    SurfaceAltitudeVelocity = GetSurfaceAltitudeVelocity();
                    _previousSeaLevelAltitude = CurrentSeaLevelAltitude;
                    CurrentSeaLevelAltitude = GetSeaLevelAltitude();
                    SeaLevelAltitudeVelocity = GetSeaLevelAltitudeVelocity();
                }

                private double GetSurfaceAltitudeVelocity() {
                    return (CurrentSurfaceAltitude - _previousSurfaceAltitude) / (1.0 / 6.0);
                }

                private double GetSeaLevelAltitudeVelocity() {
                    return (CurrentSeaLevelAltitude - _previousSeaLevelAltitude) / (1.0 / 6.0);
                }

                public bool IdleSurfaceAltitude(double targetAltitude) {
                    double surfaceAltitudeError = GetSurfaceAltitudeError(targetAltitude);
                    // if we're close enough, don't do anything
                    if (surfaceAltitudeError < ALTITUDE_ERROR_TOLERANCE) {
                        SetSurfaceVa(0, SurfaceAltitudeVelocity, targetAltitude);
                        return true;
                    }
                    return false;
                }

                public bool IdleSeaLevelAltitude(double targetAltitude) {
                    double seaLevelAltitudeError = GetSeaLevelAltitudeError(targetAltitude);
                    // if we're close enough, don't do anything
                    if (seaLevelAltitudeError < ALTITUDE_ERROR_TOLERANCE) {
                        SetSeaLevelVa(0, SeaLevelAltitudeVelocity, targetAltitude);
                        return true;
                    }
                    return false;
                }

                public bool SetSurfaceVa(double desiredVelocity, double altitudeVelocity, double targetAltitude) {
                    Vector3D counterGravityForceVector = FlightController.GetAltitudeVelocityChangeForceVector(desiredVelocity, altitudeVelocity);
                    // NOTE: counterGravityForceVector has already been transformed to local frame of reference
                    FlightController._velocityController.ExertVectorForce_LocalFrame(counterGravityForceVector, counterGravityForceVector.Length());
                    return GetSurfaceAltitudeError(targetAltitude) < ALTITUDE_ERROR_TOLERANCE;
                }

                public bool SetSeaLevelVa(double desiredVelocity, double altitudeVelocity, double targetAltitude) {
                    Vector3D counterGravityForceVector = FlightController.GetAltitudeVelocityChangeForceVector(desiredVelocity, altitudeVelocity);
                    // NOTE: counterGravityForceVector has already been transformed to local frame of reference
                    FlightController._velocityController.ExertVectorForce_LocalFrame(counterGravityForceVector, counterGravityForceVector.Length());
                    return GetSeaLevelAltitudeError(targetAltitude) < ALTITUDE_ERROR_TOLERANCE;
                }

                private double GetSurfaceAltitudeError(double targetAltitude) {
                    return Math.Abs(GetSurfaceAltitude() - targetAltitude);
                }

                private double GetSeaLevelAltitudeError(double targetAltitude) {
                    return Math.Abs(GetSeaLevelAltitude() - targetAltitude);
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

