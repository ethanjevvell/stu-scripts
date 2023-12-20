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
                    return FlightController.OrientShip(TargetPos);
                }

            }

        }
    }
}
