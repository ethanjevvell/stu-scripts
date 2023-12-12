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

                            velocityStable = FlightController.SetStableForwardVelocity(0);

                            //if (velocityStable) {
                            //    phase = LaunchPhase.SlowBurn;
                            //    break;
                            //}

                            break;


                        case LaunchPhase.SlowBurn:

                            velocityStable = FlightController.SetStableForwardVelocity(20);

                            if (velocityStable) {
                                phase = LaunchPhase.End;
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
