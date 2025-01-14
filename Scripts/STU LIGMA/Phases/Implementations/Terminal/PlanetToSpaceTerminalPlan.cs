namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class PlanetToSpaceTerminalPlan : ITerminalPlan {

                public override int TERMINAL_VELOCITY => 250;

                public override bool Run() {
                    FirstRunTasks();
                    FlightController.SetStableForwardVelocity(TERMINAL_VELOCITY);
                    FlightController.AlignShipToTarget(TargetData.Position);
                    FlightController.OptimizeShipRoll(TargetData.Position);
                    return false;
                }

            }
        }
    }
}
