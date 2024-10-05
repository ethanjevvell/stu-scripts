using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class GotoAndStop : STUStateMachine {
                public override string Name => "Goto And Stop";

                private STUFlightController FC { get; set; }

                enum GotoStates {
                    ORIENT,
                    CRUISE,
                    DECELERATE
                }

                GotoStates CurrentState { get; set; }
                Vector3D TargetPos { get; set; }
                float CruiseVelocity { get; set; }

                // +/- x-m error tolerance
                const double STOPPING_DISTANCE_ERROR_TOLERANCE = 5;

                public GotoAndStop(STUFlightController thisFlightController, Vector3D targetPos, float cruiseVelocity) {
                    FC = thisFlightController;
                    CurrentState = GotoStates.ORIENT;
                    CruiseVelocity = cruiseVelocity;
                    TargetPos = targetPos;
                }

                public override bool Init() {
                    FC.ReinstateGyroControl();
                    FC.ReinstateThrusterControl();
                    // If we're already moving, jump to cruise phase
                    CurrentState = FC.CurrentVelocity == Vector3D.Zero ? GotoStates.ORIENT : GotoStates.CRUISE;
                    CreateWarningFlightLog($"{CurrentState}");
                    return true;
                }

                public override bool Run() {

                    switch (CurrentState) {

                        case GotoStates.ORIENT:
                            double distanceToTargetPos = Vector3D.Distance(FC.CurrentPosition, TargetPos);
                            if (distanceToTargetPos < STOPPING_DISTANCE_ERROR_TOLERANCE) {
                                return true;
                            }
                            if (FC.AlignShipToTarget(TargetPos)) {
                                CurrentState = GotoStates.CRUISE;
                                CreateOkFlightLog($"Oriented to target position {TargetPos}");
                                CreateInfoFlightLog($"Will attempt to decelerate at {FC.CalculateForwardStoppingDistance() + (1.0 / 6.0) * FC.VelocityMagnitude} from targetPos");
                            }
                            break;

                        case GotoStates.CRUISE:
                            bool cruising = FC.SetStableForwardVelocity(CruiseVelocity);
                            bool aligned = FC.AlignShipToTarget(TargetPos);
                            double stoppingDistance = FC.CalculateForwardStoppingDistance();
                            distanceToTargetPos = Vector3D.Distance(FC.CurrentPosition, TargetPos);
                            CreateInfoFlightLog($"Distance to target: {distanceToTargetPos}");
                            if (distanceToTargetPos <= stoppingDistance + (1.0 / 6.0) * FC.VelocityMagnitude) {
                                CreateWarningFlightLog($"Decelerating at {stoppingDistance + (1.0 / 6.0) * FC.VelocityMagnitude}");
                                CurrentState = GotoStates.DECELERATE;
                            }
                            break;

                        case GotoStates.DECELERATE:
                            cruising = FC.SetStableForwardVelocity(0);
                            aligned = FC.AlignShipToTarget(TargetPos);
                            distanceToTargetPos = Vector3D.Distance(FC.CurrentPosition, TargetPos);
                            if (cruising && distanceToTargetPos < STOPPING_DISTANCE_ERROR_TOLERANCE) {
                                CreateOkFlightLog("reached target position " + TargetPos);
                                return true;
                            } else if (cruising) {
                                // If we're still cruising but we're not close enough to the target, we're going too fast
                                CruiseVelocity /= 2;
                                CurrentState = GotoStates.ORIENT;
                                CreateWarningFlightLog($"Recalculating cruise velocity to {CruiseVelocity}");
                            }
                            break;
                    }

                    return false;
                }

                public override bool Closeout() {
                    FC.ToggleDampeners(true);
                    if (FC.CurrentVelocity.IsZero()) {
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
