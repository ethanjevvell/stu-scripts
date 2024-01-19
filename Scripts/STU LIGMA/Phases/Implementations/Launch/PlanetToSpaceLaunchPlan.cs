using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class PlanetToSpaceLaunchPlan : ILaunchPlan {

                private double LAUNCH_VELOCITY = 200;
                private double ELEVATION_CUTOFF = 2000;
                private double CurrentElevation;

                private enum LaunchPhase {
                    Start,
                    StraightFlight,
                    CircumnavigatePlanet,
                    End
                }

                public override bool Run() {

                    FirstRunTasks();
                    FlightController.SetStableForwardVelocity(LAUNCH_VELOCITY);

                    if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out CurrentElevation)) {
                        if (CurrentElevation > ELEVATION_CUTOFF) {
                            return true;
                        }
                    }

                    return false;

                }
            }
        }
    }
}