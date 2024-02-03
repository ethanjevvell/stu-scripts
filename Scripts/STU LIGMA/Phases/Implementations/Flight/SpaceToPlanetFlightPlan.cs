
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class SpaceToPlanetFlightPlan : IFlightPlan {

                private double FLIGHT_VELOCITY = 500;
                private double ELEVATION_CUTOFF = 7500;
                private double CurrentElevation;

                public override bool Run() {

                    StraightFlight();

                    if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out CurrentElevation)) {
                        if (CurrentElevation <= ELEVATION_CUTOFF) {
                            return true;
                        }
                    }

                    return false;

                }

                private bool StraightFlight() {
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

