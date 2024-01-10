namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class SpaceToSpaceTerminalPlan : ITerminalPlan {

                private int TERMINAL_VELOCITY = 100;
                private int TERMINAL_DISTANCE = 30;

                public override bool Run() {
                    FlightController.SetStableForwardVelocity(TERMINAL_VELOCITY);
                    FlightController.AlignShipToTarget(TargetData.Position);
                    //if (Vector3D.Distance(FlightController.CurrentPosition, TargetData.Position) < TERMINAL_DISTANCE) {
                    //    SelfDestruct();
                    //    return true;
                    //}
                    return false;
                }

            }
        }
    }
}