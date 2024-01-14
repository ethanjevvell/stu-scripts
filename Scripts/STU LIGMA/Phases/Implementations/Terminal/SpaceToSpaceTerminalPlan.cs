namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class SpaceToSpaceTerminalPlan : ITerminalPlan {

                private int TERMINAL_VELOCITY = 170;

                public override bool Run() {
                    FlightController.SetStableForwardVelocity(TERMINAL_VELOCITY);
                    FlightController.AlignShipToTarget(TargetData.Position);
                    return false;
                }

            }
        }
    }
}