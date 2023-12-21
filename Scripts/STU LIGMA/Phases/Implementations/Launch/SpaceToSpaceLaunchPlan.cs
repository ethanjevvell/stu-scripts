namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class SpaceToSpaceLaunchPlan : ILaunchPlan {

                private enum LaunchPhase {
                    Idle,
                    Start,
                    End
                };

                private bool velocityStable = false;
                private static LaunchPhase phase = LaunchPhase.Idle;
                private double LAUNCH_VELOCITY = 250;

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
                            velocityStable = FlightController.SetStableForwardVelocity(LAUNCH_VELOCITY);

                            if (velocityStable) {
                                phase = LaunchPhase.End;
                                break;
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
