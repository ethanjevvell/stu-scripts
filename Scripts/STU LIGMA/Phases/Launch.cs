using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public class Launch {

                // Temporary; for ensuring missile is far enough from test site before self destruct
                private const double SELF_DESTRUCT_THRESHOLD = 3000;

                public enum LaunchPhase {
                    Idle,
                    InitialBurn,
                    SlowBurn,

                    Terminal
                }

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static void Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:

                            phase = LaunchPhase.InitialBurn;
                            break;

                        case LaunchPhase.InitialBurn:

                            Maneuvers.SetForwardVelocity(70);
                            var distance = Vector3D.Distance(StartPosition, CurrentPosition);
                            if (distance > SELF_DESTRUCT_THRESHOLD) {
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
