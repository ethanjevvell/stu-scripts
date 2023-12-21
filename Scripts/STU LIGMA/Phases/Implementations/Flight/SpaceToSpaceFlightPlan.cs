using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class SpaceToSpaceFlightPlan : IFlightPlan {

                Vector3D LaunchPos;
                Vector3D TargetPos;

                public SpaceToSpaceFlightPlan(Vector3D launchPos, Vector3D targetPos) {
                    LaunchPos = launchPos;
                    TargetPos = targetPos;
                }

                public override bool Run() {
                    // REMOVE ZERO VECTOR WHEN DONE TESTING
                    FlightController.AdjustShipRoll(TargetPos, Vector3D.Zero);
                    var velocityStable = FlightController.SetStableForwardVelocity(150);
                    var orientationStable = FlightController.OrientShip(TargetPos);
                    if (velocityStable && orientationStable) {
                        FlightController.SetRoll(0);
                        return true;
                    }
                    return false;
                }

            }

        }
    }
}
