using System;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public partial class Maneuvers {

                private static PID VelocityPID = new PID(1.0, 0.3, 0.5, Runtime.TimeSinceLastRun.TotalSeconds);
                private static double VelocityErrorTolerance = 1.0;

                public static void SetForwardVelocity(float desiredVelocity) {
                    var error = desiredVelocity - Velocity;

                    if (Math.Abs(error) > VelocityErrorTolerance) {
                        Accelerate(VelocityPID.Control(error));
                    } else {
                        Accelerate(0);
                    }
                }

                public static void Accelerate(double controllerOut) {
                    if (controllerOut == 0) {
                        SetAllThrusters(0.0f);
                        return;
                    }

                    var force = Mass * controllerOut;
                    ApplyThrust(controllerOut, force);
                }

                private static void ApplyThrust(double direction, double force) {
                    if (direction < 0) {
                        var thrust = Math.Abs(force / ReverseThrusters.Length);
                        SetThrusters(ForwardThrusters, 0.0f);
                        SetThrusters(ReverseThrusters, (float)thrust);
                    } else {
                        var thrust = Math.Abs(force / ForwardThrusters.Length);
                        SetThrusters(ReverseThrusters, 0.0f);
                        SetThrusters(ForwardThrusters, (float)thrust);
                    }
                }

                private static void SetThrusters(LIGMAThruster[] thrusters, float thrust) {
                    Array.ForEach(thrusters, thruster => thruster.SetThrust(thrust));
                }

                private static void SetAllThrusters(float thrust) {
                    SetThrusters(ForwardThrusters, thrust);
                    SetThrusters(ReverseThrusters, thrust);
                }
            }
        }
    }
}
