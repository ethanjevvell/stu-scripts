using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class GotoAndStop : STUStateMachine {
                public override string Name => "Go-to-and-stop";

                private STUFlightController _flightController { get; set; }

                enum GotoStates {
                    CRUISE,
                    DECELERATE,
                    FINE_TUNE
                }

                GotoStates _currentState;
                GotoStates CurrentState {
                    get { return _currentState; }
                    set { CreateInfoFlightLog($"{Name} transitioning to {value}"); _currentState = value; }
                }

                Vector3D _targetPos { get; set; }
                IMyTerminalBlock _reference { get; set; }
                public double CruiseVelocity {
                    get { return _cruiseVelocity; }
                    set { _cruiseVelocity = value; }
                }
                double _cruiseVelocity;

                // +/- x-m error tolerance
                const double STOPPING_DISTANCE_ERROR_TOLERANCE = 1;
                const double FINE_TUNING_VELOCITY_ERROR_TOLERANCE = 0.1;

                public GotoAndStop(STUFlightController thisFlightController, Vector3D targetPos, double cruiseVelocity, IMyTerminalBlock reference = null) {
                    _flightController = thisFlightController;
                    CruiseVelocity = cruiseVelocity;
                    _targetPos = targetPos;
                    _reference = reference == null ? _flightController.RemoteControl : reference;
                }

                public override bool Init() {
                    _flightController.ReinstateGyroControl();
                    _flightController.ReinstateThrusterControl();
                    _flightController.ToggleThrusters(true);
                    _flightController.UpdateShipMass();
                    CurrentState = GotoStates.CRUISE;
                    return true;
                }

                public override bool Run() {

                    Vector3D currentPos;

                    if (_reference is IMyShipConnector) {
                        currentPos = _reference.GetPosition() + _reference.WorldMatrix.Forward * 1.25;
                    } else if (_reference is IMyRemoteControl) {
                        currentPos = _reference.CubeGrid.WorldVolume.Center;
                    } else {
                        currentPos = _reference.GetPosition();
                    }

                    double distanceToTargetPos = Vector3D.Distance(currentPos, _targetPos);
                    double currentVelocity = _flightController.CurrentVelocity_WorldFrame.Length();

                    Vector3D weakestVector = _flightController._velocityController.MinimumThrustVector;
                    double reverseAcceleration = weakestVector.Length() / STUVelocityController.ShipMass;
                    double stoppingDistance = _flightController.CalculateStoppingDistance(reverseAcceleration, currentVelocity);

                    switch (CurrentState) {

                        case GotoStates.CRUISE:

                            _flightController.SetV_WorldFrame(_targetPos, CruiseVelocity, currentPos);

                            if (distanceToTargetPos <= stoppingDistance + (1.0 / 6.0) * _flightController.CurrentVelocity_WorldFrame.Length()) {
                                _flightController._velocityController.ExertVectorForce_WorldFrame(-_flightController.CurrentVelocity_WorldFrame, _flightController._velocityController.MinimumThrustVector.Length());
                                CurrentState = GotoStates.DECELERATE;
                            }

                            break;

                        case GotoStates.DECELERATE:

                            _flightController._velocityController.ExertVectorForce_WorldFrame(-_flightController.CurrentVelocity_WorldFrame, _flightController._velocityController.MinimumThrustVector.Length());

                            if (currentVelocity <= (1.0 / 6.0) * reverseAcceleration) {
                                CurrentState = GotoStates.FINE_TUNE;
                            }
                            break;


                        case GotoStates.FINE_TUNE:

                            _flightController.SetV_WorldFrame(_targetPos, MathHelper.Min(CruiseVelocity / 2, distanceToTargetPos), currentPos);

                            if (_flightController.VelocityMagnitude < FINE_TUNING_VELOCITY_ERROR_TOLERANCE && distanceToTargetPos < STOPPING_DISTANCE_ERROR_TOLERANCE) {
                                CreateOkFlightLog($"Arrived at +/- {Math.Round(STOPPING_DISTANCE_ERROR_TOLERANCE, 2)}m from the desired destination");
                                return true;
                            }
                            break;

                    }

                    return false;
                }

                public override bool Closeout() {
                    _flightController.SetStableForwardVelocity(0);
                    if (Math.Abs(_flightController.VelocityMagnitude) <= 0.1) {
                        _flightController.RelinquishGyroControl();
                        CreateOkFlightLog($"{Name} finished");
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
