
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {
            public class Launch {

                // Temporary; for ensuring missile is far enough from test site before self destruct
                private const double SELF_DESTRUCT_THRESHOLD = 500;

                public enum LaunchPhase {
                    Idle,
                    StartBurn,
                    FirstBurnCoast,
                    Terminal
                }

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static void Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:
                            phase = LaunchPhase.StartBurn;
                            break;

                        case LaunchPhase.StartBurn:
                            StartBurn();
                            phase = LaunchPhase.FirstBurnCoast;
                            break;

                        case LaunchPhase.FirstBurnCoast:
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

                public static void StartBurn() {
                    Array.ForEach(Thrusters, thruster => thruster.Thruster.ThrustOverride = thruster.Thruster.MaxThrust);
                }

            }
        }
    }
}
