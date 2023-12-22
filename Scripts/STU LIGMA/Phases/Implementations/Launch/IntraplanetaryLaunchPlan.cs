using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class IntraplanetaryLaunchPlan : ILaunchPlan {

                private enum LaunchPhase {
                    Idle,
                    Start,
                    End
                };

                private static LaunchPhase phase = LaunchPhase.Idle;
                private double LAUNCH_VELOCITY = 150;
                private double CurrentElevation;

                public override bool Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:

                            phase = LaunchPhase.Start;
                            Broadcaster.Log(new STULog {
                                Sender = MissileName,
                                Message = "Starting launch burn",
                                Type = STULogType.WARNING,
                                Metadata = GetTelemetryDictionary()
                            });

                            break;

                        case LaunchPhase.Start:

                            FirstRunTasks();
                            FlightController.SetStableForwardVelocity(LAUNCH_VELOCITY);

                            if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out CurrentElevation)) {
                                if (CurrentElevation > 1000) {
                                    phase = LaunchPhase.End;
                                    break;
                                }
                            }

                            break;

                        case LaunchPhase.End:

                            return true;

                    }

                    return false;

                }

            }
        }
    }
}
