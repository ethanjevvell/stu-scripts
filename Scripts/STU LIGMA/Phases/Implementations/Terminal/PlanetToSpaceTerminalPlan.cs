namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class PlanetToSpaceTerminalPlan : ITerminalPlan {

                private int TERMINAL_VELOCITY = 250;

                public override bool Run() {
                    FlightController.SetStableForwardVelocity(TERMINAL_VELOCITY);
                    FlightController.AlignShipToTarget(TargetData.Position);
                    FlightController.OptimizeShipRoll(TargetData.Position);
                    return false;
                }

            }
        }
    }
}
