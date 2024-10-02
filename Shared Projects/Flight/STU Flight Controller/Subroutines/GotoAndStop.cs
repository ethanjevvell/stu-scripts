namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class GotoAndStop : STUStateMachine {
                public override string Name => "Goto And Stop";

                private double oneTickAcceleration;
                private STUFlightController FC;

                public GotoAndStop(STUFlightController thisFlightController) {
                    oneTickAcceleration = 0;
                    FC = thisFlightController;
                }

                public override bool Init() {
                    return true;
                }

                public override bool Run() {
                    return true;
                }

                public override bool Closeout() {
                    return true;
                }
            }
        }
    }
}