namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class InterplanetaryTerminalPlan : ITerminalPlan {

                private int TERMINAL_VELOCITY = 200;

                public override bool Run() {
                    FlightController.SetStableForwardVelocity(TERMINAL_VELOCITY);
                    FlightController.AlignShipToTarget(TargetData.Position);
                    return false;
                }

            }
        }
    }
}

