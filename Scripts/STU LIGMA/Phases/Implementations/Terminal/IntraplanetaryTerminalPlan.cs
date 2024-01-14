
namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class IntraplanetaryTerminalPlan : ITerminalPlan {

                private int TERMINAL_VELOCITY = 120;

                public override bool Run() {
                    FlightController.SetStableForwardVelocity(TERMINAL_VELOCITY);
                    FlightController.AlignShipToTarget(TargetData.Position);
                    return false;
                }

            }
        }
    }
}
