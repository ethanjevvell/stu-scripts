namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class SpaceToSpaceTerminalPlan : ITerminalPlan {

                public override int TERMINAL_VELOCITY => 200;

                public override bool Run() {
                    FirstRunTasks();
                    FlightController.SetStableForwardVelocity(TERMINAL_VELOCITY);
                    FlightController.AlignShipToTarget(TargetData.Position);
                    return false;
                }

            }
        }
    }
}