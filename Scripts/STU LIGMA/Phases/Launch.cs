
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
                    InitialBurn,
                    Terminal
                }

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static void Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:
                            phase = LaunchPhase.InitialBurn;
                            break;

                        case LaunchPhase.InitialBurn:
                            InitialBurn();
                            break;

                        case LaunchPhase.Terminal:
                            SelfDestruct();
                            break;

                    }

                }

                public static void InitialBurn() {
                    Array.ForEach(Thrusters, thruster => thruster.Thruster.ThrustOverride = thruster.Thruster.MaxThrust);
                    var distance = Vector3D.Distance(StartPosition, CurrentPosition);
                    if (distance > SELF_DESTRUCT_THRESHOLD) {
                        Broadcaster.Log(new STULog {
                            Sender = "LIGMA Missile",
                            Message = $"SELF-DESTRUCT: Distance detected = {distance}",
                            Type = STULogType.OK
                        });
                        phase = LaunchPhase.Terminal;
                    }
                }

            }
        }
    }
}
