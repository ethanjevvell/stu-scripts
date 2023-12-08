using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public class Launch {

                // Temporary; for ensuring missile is far enough from test site before self destruct
                private const double SELF_DESTRUCT_THRESHOLD = 3000;

                private static Vector3D TestTarget = new Vector3D(581.42, -344.59, -897.14);

                public enum LaunchPhase {
                    Idle,
                    InitialBurn,
                    Flight,
                    Terminal
                }

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static void Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:

                            phase = LaunchPhase.InitialBurn;
                            Broadcaster.Log(new STULog {
                                Sender = MissileName,
                                Message = "Starting initial burn",
                                Type = STULogType.OK,
                                Metadata = GetTelemetryDictionary()
                            });

                            break;

                        case LaunchPhase.InitialBurn:

                            var alignmentComplete = Maneuvers.Orientation.AlignGyro(TestTarget);

                            if (alignmentComplete) {
                                Broadcaster.Log(new STULog {
                                    Sender = MissileName,
                                    Message = "Entering flight phase",
                                    Type = STULogType.OK,
                                    Metadata = GetTelemetryDictionary()
                                });
                                phase = LaunchPhase.Flight;
                            }

                            break;

                        case LaunchPhase.Flight:

                            Maneuvers.Velocity.ControlForward(70);
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
