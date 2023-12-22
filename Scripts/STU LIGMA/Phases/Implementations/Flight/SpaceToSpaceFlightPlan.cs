using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class SpaceToSpaceFlightPlan : IFlightPlan {

                private Vector3D LaunchPos;
                private Vector3D TargetPos;
                private const double FLIGHT_VELOCITY = 200;

                public SpaceToSpaceFlightPlan(Vector3D launchPos, Vector3D targetPos) {
                    LaunchPos = launchPos;
                    TargetPos = targetPos;
                }

                public override bool Run() {
                    // REMOVE ZERO VECTOR WHEN DONE TESTING
                    FlightController.OptimizeShipRoll(TargetPos, Vector3D.Zero);
                    FlightController.AlignShipToTarget(TargetPos);
                    var velocityStable = FlightController.SetStableForwardVelocity(FLIGHT_VELOCITY);
                    if (velocityStable) {
                        FlightController.SetVr(0);
                        return true;
                    }
                    return false;
                }

            }

        }
    }
}
