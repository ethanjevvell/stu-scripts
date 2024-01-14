using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class IntraplanetaryDescentPlan : IDescentPlan {

                private double DESCENT_VELOCITY = 200;
                private double ELEVATION_CUTOFF = 2000;
                private double CurrentElevation;

                public override bool Run() {

                    FirstRunTasks();
                    FlightController.AlignShipToTarget(TargetData.Position);
                    FlightController.OptimizeShipRoll(TargetData.Position);
                    FlightController.SetStableForwardVelocity(DESCENT_VELOCITY);

                    if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out CurrentElevation)) {
                        if (CurrentElevation <= ELEVATION_CUTOFF) {
                            return true;
                        }
                    }

                    return false;

                }
            }
        }
    }
}
