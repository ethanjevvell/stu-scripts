namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class Launch {

                public enum LaunchPhase {
                    Idle,
                    FastBurn,
                    SlowBurn,
                    End
                }

                private static bool velocityStable = false;

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static bool Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:

                            phase = LaunchPhase.FastBurn;
                            Broadcaster.Log(new STULog {
                                Sender = MissileName,
                                Message = "Starting initial burn",
                                Type = STULogType.WARNING,
                                Metadata = GetTelemetryDictionary()
                            });

                            break;

                        case LaunchPhase.FastBurn:

                            velocityStable = FlightController.SetStableForwardVelocity(100);

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
