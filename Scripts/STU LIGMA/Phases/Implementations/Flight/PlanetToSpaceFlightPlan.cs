namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class PlanetToSpaceFlightPlan : IFlightPlan {

                private const double FLIGHT_VELOCITY = 150;

                public override bool Run() {
                    FlightController.OptimizeShipRoll(TargetData.Position);
                    FlightController.SetStableForwardVelocity(FLIGHT_VELOCITY);
                    var shipAligned = FlightController.AlignShipToTarget(TargetData.Position);

                    if (shipAligned) {
                        return true;
                    }

                    return false;
                }

            }

        }
    }
}

