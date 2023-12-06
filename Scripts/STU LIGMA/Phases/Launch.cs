namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public class Launch {

                // Temporary; for ensuring missile is far enough from test site before self destruct
                private const double SELF_DESTRUCT_THRESHOLD = 3000;

                public enum LaunchPhase {
                    Idle,
                    RightBurn,
                    LeftBurn,
                    Terminal
                }

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static void Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:

                            phase = LaunchPhase.RightBurn;
                            break;

                        case LaunchPhase.RightBurn:

                            var forwardReached = Maneuvers.Velocity.ControlForward(80);
                            var rightReached = Maneuvers.Velocity.ControlRight(10);
                            if (forwardReached && rightReached) {
                                phase = LaunchPhase.LeftBurn;
                            }
                            break;

                        case LaunchPhase.LeftBurn:

                            var forwardReached2 = Maneuvers.Velocity.ControlForward(-20);
                            var rightReached2 = Maneuvers.Velocity.ControlRight(-10);
                            if (forwardReached2 && rightReached2) {
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
