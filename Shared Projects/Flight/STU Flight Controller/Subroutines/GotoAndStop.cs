using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class GotoAndStop : STUStateMachine {
                public override string Name => "Goto And Stop";

                private STUFlightController FC { get; set; }

                enum GotoStates {
                    CRUISE,
                    DECELERATE,
                    FINE_TUNE
                }

                GotoStates CurrentState { get; set; }
                Vector3D TargetPos { get; set; }
                double CruiseVelocity { get; set; }

                // +/- x-m error tolerance
                const double STOPPING_DISTANCE_ERROR_TOLERANCE = 1.0 / 4.0;
                const double FINE_TUNING_VELOCITY_ERROR_TOLERANCE = 0.1;

                public GotoAndStop(STUFlightController thisFlightController, Vector3D targetPos, double cruiseVelocity) {
                    FC = thisFlightController;
                    CurrentState = GotoStates.CRUISE;
                    CruiseVelocity = cruiseVelocity;
                    TargetPos = targetPos;
                }

                public override bool Init() {
                    FC.ReinstateGyroControl();
                    FC.ReinstateThrusterControl();
                    CreateInfoFlightLog("Initiating GotoAndStop maneuver");
                    return true;
                }

                public override bool Run() {

                    Vector3D currentPos = FC.RemoteControl.CubeGrid.WorldVolume.Center;
                    double distanceToTargetPos = Vector3D.Distance(currentPos, TargetPos);
                    double currentVelocity = FC.CurrentVelocity_WorldFrame.Length();

                    Vector3D weakestVector = FC.VelocityController.MinimumThrustVector;
                    double reverseAcceleration = weakestVector.Length() / STUVelocityController.ShipMass;
                    double stoppingDistance = FC.CalculateStoppingDistance(reverseAcceleration, currentVelocity);

                    switch (CurrentState) {

                        case GotoStates.CRUISE:

                            FC.SetV_WorldFrame(TargetPos, CruiseVelocity, currentPos);

                            if (distanceToTargetPos <= stoppingDistance + (1.0 / 6.0) * FC.CurrentVelocity_WorldFrame.Length()) {
                                CreateInfoFlightLog("CRUISE exit condition satisfied, moving to DECELERATE");
                                FC.VelocityController.ExertVectorForce_WorldFrame(-FC.CurrentVelocity_WorldFrame, FC.VelocityController.MinimumThrustVector.Length());
                                CurrentState = GotoStates.DECELERATE;
                            }

                            break;

                        case GotoStates.DECELERATE:

                            FC.VelocityController.ExertVectorForce_WorldFrame(-FC.CurrentVelocity_WorldFrame, FC.VelocityController.MinimumThrustVector.Length());

                            if (currentVelocity <= (1.0 / 6.0) * reverseAcceleration) {
                                CreateInfoFlightLog("Fine tuning...");
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
                    if (Math.Abs(FC.VelocityMagnitude) < 1e-2) {
                        FC.RelinquishGyroControl();
                        CreateOkBroadcast("GotoAndStop maneuver complete");
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
