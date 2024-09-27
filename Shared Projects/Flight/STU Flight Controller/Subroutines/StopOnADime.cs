namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class StopOnADime : ManeuverTemplate {
                public override string Name => "Stop On A Dime";

                private double oneTickAcceleration;
                private STUFlightController FC;

                public StopOnADime(STUFlightController thisFlightController) {
                    oneTickAcceleration = 0;
                    FC = thisFlightController;
                }

                protected override bool Init() {
                    return true;
                }

                protected override bool Run() {
                    return true;
                }

                protected override bool Closeout() {
                    return true;
                }
            }
        }
    }
}