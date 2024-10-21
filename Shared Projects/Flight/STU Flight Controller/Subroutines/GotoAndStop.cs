using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class GotoAndStop : STUStateMachine {
                public override string Name => "Go-to-and-stop";

                private STUFlightController FC { get; set; }

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

                Vector3D TargetPos { get; set; }
                IMyTerminalBlock Reference { get; set; }
                double CruiseVelocity { get; set; }

                // +/- x-m error tolerance
                const double STOPPING_DISTANCE_ERROR_TOLERANCE = 1;
                const double FINE_TUNING_VELOCITY_ERROR_TOLERANCE = 0.1;

                public GotoAndStop(STUFlightController thisFlightController, Vector3D targetPos, double cruiseVelocity, IMyTerminalBlock reference = null) {
                    FC = thisFlightController;
                    CruiseVelocity = cruiseVelocity;
                    TargetPos = targetPos;
                    Reference = reference == null ? FC.RemoteControl : reference;
                }

                public override bool Init() {
                    FC.ReinstateGyroControl();
                    FC.ReinstateThrusterControl();
                    FC.ToggleThrusters(true);
                    FC.UpdateShipMass();
                    CurrentState = GotoStates.CRUISE;
                    return true;
                }

                public override bool Run() {

                    Vector3D currentPos;

                    if (Reference is IMyShipConnector) {
                        currentPos = Reference.GetPosition() + Reference.WorldMatrix.Forward * 1.25;
                    } else if (Reference is IMyRemoteControl) {
                        currentPos = Reference.CubeGrid.WorldVolume.Center;
                    } else {
                        currentPos = Reference.GetPosition();
                    }

                    double distanceToTargetPos = Vector3D.Distance(currentPos, TargetPos);
                    double currentVelocity = FC.CurrentVelocity_WorldFrame.Length();

                    Vector3D weakestVector = FC._velocityController.MinimumThrustVector;
                    double reverseAcceleration = weakestVector.Length() / STUVelocityController.ShipMass;
                    double stoppingDistance = FC.CalculateStoppingDistance(reverseAcceleration, currentVelocity);

                    switch (CurrentState) {

                        case GotoStates.CRUISE:

                            FC.SetV_WorldFrame(TargetPos, CruiseVelocity, currentPos);

                            if (distanceToTargetPos <= stoppingDistance + (1.0 / 6.0) * FC.CurrentVelocity_WorldFrame.Length()) {
                                FC._velocityController.ExertVectorForce_WorldFrame(-FC.CurrentVelocity_WorldFrame, FC._velocityController.MinimumThrustVector.Length());
                                CurrentState = GotoStates.DECELERATE;
                            }

                            break;

                        case GotoStates.DECELERATE:

                            FC._velocityController.ExertVectorForce_WorldFrame(-FC.CurrentVelocity_WorldFrame, FC._velocityController.MinimumThrustVector.Length());

                            if (currentVelocity <= (1.0 / 6.0) * reverseAcceleration) {
                                CurrentState = GotoStates.FINE_TUNE;
                            }
                            break;


                        case GotoStates.FINE_TUNE:

                            FC.SetV_WorldFrame(TargetPos, MathHelper.Min(CruiseVelocity / 2, distanceToTargetPos), currentPos);

                            if (FC.VelocityMagnitude < FINE_TUNING_VELOCITY_ERROR_TOLERANCE && distanceToTargetPos < STOPPING_DISTANCE_ERROR_TOLERANCE) {
                                CreateOkFlightLog($"Arrived at +/- {Math.Round(STOPPING_DISTANCE_ERROR_TOLERANCE, 2)}m from the desired destination");
                                return true;
                            }
                            break;

                    }

                    return false;
                }

                public override bool Closeout() {
                    FC.SetStableForwardVelocity(0);
                    if (Math.Abs(FC.VelocityMagnitude) <= 0.1) {
                        FC.RelinquishGyroControl();
                        CreateOkFlightLog($"{Name} finished");
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
