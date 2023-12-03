using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public class Launch {

                // Temporary; for ensuring missile is far enough from test site before self destruct
                private const double SELF_DESTRUCT_THRESHOLD = 3000;

                private static PID InitialBurnPID = new PID(1.2, 0.1, 0.1, 1.0 / 6.0);
                private static float InitialBurnTargetVelocity = 70;

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
                            SetVelocity(70);
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

                public static void SetVelocity(float desiredVelocity) {
                    var error = desiredVelocity - Velocity;
                    SetAcceleration((float)InitialBurnPID.Control(error));
                }

                public static void SetAcceleration(double a) {
                    var force = Mass * a;
                    var thrustPerForwardThruster = Math.Abs(force / ForwardThrusters.Length);
                    var thrustPerReverseThruster = Math.Abs(force / ReverseThrusters.Length);

                    // if the error is negative, we are traveling faster than the target velocity
                    // and must engage reverse thrusters
                    if (a < 0) {
                        Array.ForEach(ForwardThrusters, thruster => thruster.Thruster.ThrustOverride = 0);
                        Array.ForEach(ReverseThrusters, thruster => thruster.Thruster.ThrustOverride = (float)thrustPerReverseThruster);
                    } else {
                        Array.ForEach(ReverseThrusters, thruster => thruster.Thruster.ThrustOverride = 0);
                        Array.ForEach(ForwardThrusters, thruster => thruster.Thruster.ThrustOverride = (float)thrustPerForwardThruster);
                    }

                }

            }
        }
    }
}
