using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public class Launch {

                // Temporary; for ensuring missile is far enough from test site before self destruct
                private const double SELF_DESTRUCT_THRESHOLD = 3000;

                private static Vector3D TestTarget = new Vector3D(484.92, 3816.29, -1834.40);

                public enum LaunchPhase {
                    Idle,
                    InitialBurn,
                    TurnBurn,
                    Flight,
                    Terminal
                }

                private static bool VzSet = false;
                private static bool VxSet = false;
                private static bool VySet = false;
                private static bool orientationSet = false;

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static void Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:

                            phase = LaunchPhase.InitialBurn;
                            Broadcaster.Log(new STULog {
                                Sender = MissileName,
                                Message = "Starting initial burn",
                                Type = STULogType.WARNING,
                                Metadata = GetTelemetryDictionary()
                            });

                            break;

                        case LaunchPhase.InitialBurn:

                            VzSet = Maneuvers.Velocity.ControlForward(100);
                            VxSet = Maneuvers.Velocity.ControlRight(0);
                            VySet = Maneuvers.Velocity.ControlUp(0);

                            if (VzSet && VxSet && VySet) {
                                Broadcaster.Log(new STULog {
                                    Sender = MissileName,
                                    Message = "Entering turn phase",
                                    Type = STULogType.WARNING,
                                    Metadata = GetTelemetryDictionary()
                                });
                                phase = LaunchPhase.TurnBurn;
                            }

                            break;

                        case LaunchPhase.TurnBurn:
                            orientationSet = Maneuvers.Orientation.AlignGyro(TestTarget);
                            VzSet = Maneuvers.Velocity.ControlForward(60);
                            VxSet = Maneuvers.Velocity.ControlRight(0);
                            VySet = Maneuvers.Velocity.ControlUp(0);

                            if (orientationSet && VzSet && VxSet && VySet) {
                                Broadcaster.Log(new STULog {
                                    Sender = MissileName,
                                    Message = "Entering flight phase",
                                    Type = STULogType.WARNING,
                                    Metadata = GetTelemetryDictionary()
                                });
                                phase = LaunchPhase.Flight;
                            }
                            break;


                        case LaunchPhase.Flight:

                            Maneuvers.Orientation.AlignGyro(TestTarget);
                            Maneuvers.Velocity.ControlForward(500);
                            Maneuvers.Velocity.ControlRight(0);
                            Maneuvers.Velocity.ControlUp(0);

                            if (Vector3D.Distance(CurrentPosition, TestTarget) < 15) {
                                phase = LaunchPhase.Terminal;
                            }

                            break;

                        case LaunchPhase.Terminal:

                            SelfDestruct();
                            break;

                    }

                }


            }
        }
    }
}
