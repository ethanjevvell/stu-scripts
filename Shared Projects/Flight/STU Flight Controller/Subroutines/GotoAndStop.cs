using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class GotoAndStop : STUStateMachine {
                public override string Name => "Goto And Stop";

                private STUFlightController FC { get; set; }

                enum GotoStates {
                    CRUISE,
                    DECELERATE
                }

                GotoStates CurrentState { get; set; }
                Vector3D TargetPos { get; set; }
                double CruiseVelocity { get; set; }

                // +/- x-m error tolerance
                const double STOPPING_DISTANCE_ERROR_TOLERANCE = 5;

                public GotoAndStop(STUFlightController thisFlightController, Vector3D targetPos, double cruiseVelocity) {
                    FC = thisFlightController;
                    CurrentState = GotoStates.CRUISE;
                    CruiseVelocity = cruiseVelocity;
                    TargetPos = targetPos;
                }

                public override bool Init() {
                    FC.ReinstateGyroControl();
                    FC.ReinstateThrusterControl();
                    return true;
                }

                public override bool Run() {

                    switch (CurrentState) {

                        case GotoStates.CRUISE:
                            bool cruising = FC.SetV_WorldFrame(TargetPos, CruiseVelocity);
                            double stoppingDistance = FC.CalculateForwardStoppingDistance();
                            double distanceToTargetPos = Vector3D.Distance(FC.CurrentPosition, TargetPos);
                            CreateInfoFlightLog($"Distance to target: {distanceToTargetPos}");
                            if (distanceToTargetPos <= stoppingDistance + (1.0 / 6.0) * FC.VelocityMagnitude) {
                                CreateWarningFlightLog($"Decelerating at {stoppingDistance + (1.0 / 6.0) * FC.VelocityMagnitude}");
                                CurrentState = GotoStates.DECELERATE;
                            }
                            break;

                        case GotoStates.DECELERATE:
                            cruising = FC.SetStableForwardVelocity(0);
                            distanceToTargetPos = Vector3D.Distance(FC.CurrentPosition, TargetPos);
                            if (cruising && distanceToTargetPos < STOPPING_DISTANCE_ERROR_TOLERANCE) {
                                CreateOkFlightLog("reached target position " + TargetPos);
                                return true;
                            } else if (cruising) {
                                // If we're still cruising but we're not close enough to the target, we're going too fast
                                CruiseVelocity /= 2;
                                CurrentState = GotoStates.CRUISE;
                                CreateWarningFlightLog($"Recalculating cruise velocity to {CruiseVelocity}");
                            }
                            break;
                    }

                    return false;
                }

                public override bool Closeout() {
                    FC.ToggleDampeners(true);
                    if (FC.CurrentVelocity_WorldFrame.IsZero()) {
                        CreateOkFlightLog("Returning controls to user");
                        FC.RelinquishGyroControl();
                        FC.RelinquishThrusterControl();
                        FC.ToggleDampeners(false);
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
