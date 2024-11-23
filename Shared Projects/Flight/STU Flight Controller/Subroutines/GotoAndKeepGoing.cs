using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class GotoAndKeepGoing : STUStateMachine {
                public override string Name => "Goto & Keep Going";

                private STUFlightController FC;
                private Vector3D TargetPos;
                private float CruiseVelocity;
                private double DistanceOnLastTick;

                public GotoAndKeepGoing(STUFlightController thisFlightController, Vector3D targetPos, float cruiseVelocity) {
                    FC = thisFlightController;
                    TargetPos = targetPos;
                    CruiseVelocity = cruiseVelocity;
                }

                public override bool Init() {
                    FC.ReinstateThrusterControl();
                    FC.ReinstateGyroControl();
                    DistanceOnLastTick = Vector3D.Distance(FC.CurrentPosition, TargetPos);
                    return true;
                }

                public override bool Run() {
                    FC.SetV_WorldFrame(TargetPos, CruiseVelocity);

                    if (Vector3D.Distance(FC.CurrentPosition, TargetPos) > DistanceOnLastTick) { return true; } else { return false; }
                }

                public override bool Closeout() {
                    CreateOkFlightLog("Hit waypoint, just passing through...");
                    return true;
                }
            }
        }
    }
}