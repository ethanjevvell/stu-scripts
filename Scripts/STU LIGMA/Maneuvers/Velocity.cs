using System;

namespace IngameScript {
    partial class Program {
        public partial class Missile {
            public partial class Maneuvers {

                public partial class Velocity {

                    private static VelocityController ForwardController;
                    private static VelocityController ReverseController;
                    private static VelocityController LeftController;
                    private static VelocityController RightController;
                    private static VelocityController UpController;
                    private static VelocityController DownController;

                    private const double dt = TimeStep;

                    /// <summary>
                    /// The maximum acceleration possible for each direction. F = ma => a = F/m
                    /// </summary>
                    private class A_max {
                        public static double Forward = MaximumForwardThrust / TotalMissileMass;
                        public static double Reverse = MaximumReverseThrust / TotalMissileMass;
                        public static double Left = MaximumLeftThrust / TotalMissileMass;
                        public static double Right = MaximumRightThrust / TotalMissileMass;
                        public static double Up = MaximumUpThrust / TotalMissileMass;
                        public static double Down = MaximumDownThrust / TotalMissileMass;
                    }

                    /// <summary>
                    /// A coefficient for the dt between each script run. `N` should be at the very minimum greater than 1.
                    /// Higher values of `N` will result in smoother deceleration, but LIGMA will cover more ground before achieving the desired velocity.
                    /// Lower values of `N` will result in more abrupt deceleration.
                    /// </summary>
                    private class N {
                        public static double Forward = 6;
                        public static double Reverse = 6;
                        public static double Left = 6;
                        public static double Right = 6;
                        public static double Up = 6;
                        public static double Down = 6;
                    }

                    /// <summary>
                    /// Controls how early the thrusters will begin to decelerate. V_buffer = 35 means LIGMA will begin decelerating when it is 35m/s away from the desired velocity.
                    /// </summary>
                    private class V_buffer {
                        public static double Forward = A_max.Forward * dt * N.Forward;
                        public static double Reverse = A_max.Reverse * dt * N.Reverse;
                        public static double Left = A_max.Left * dt * N.Left;
                        public static double Right = A_max.Right * dt * N.Right;
                        public static double Up = A_max.Up * dt * N.Up;
                        public static double Down = A_max.Down * dt * N.Down;
                    }

                    /// <summary>
                    /// Velocity controller utility. Handles acceleration and deceleration automatically based on desired velocity; deceleration occurs with a roughly natural decay.
                    /// </summary>
                    private class VelocityController {

                        private double v_buffer;
                        private double N;
                        private double decelerationInterval;

                        private LIGMAThruster[] A_Thrusters;
                        private LIGMAThruster[] B_Thrusters;
                        private double MaximumAThrust;
                        private double MaximumBThrust;

                        private bool Cruising = false;
                        private bool AtMaxAcceleration = false;

                        public VelocityController(double buffer, double n, LIGMAThruster[] aThrusters, double maxAThrust, LIGMAThruster[] bThrusters, double maxBThrust) {
                            v_buffer = buffer;
                            N = n;
                            decelerationInterval = dt * N;
                            A_Thrusters = aThrusters;
                            MaximumAThrust = maxAThrust;
                            B_Thrusters = bThrusters;
                            MaximumBThrust = maxBThrust;
                        }

                        public void SetVelocity(double desiredVelocity) {
                            double velocityDiff = desiredVelocity - Missile.Velocity;

                            if (!Cruising) {
                                ToggleThrusters(A_Thrusters, true);
                                ToggleThrusters(B_Thrusters, true);
                            }

                            if (velocityDiff > v_buffer) {
                                if (!AtMaxAcceleration) {
                                    SetThrusterOverrides(A_Thrusters, MaximumAThrust);
                                    AtMaxAcceleration = true;
                                }
                                return;
                            }

                            AtMaxAcceleration = false;
                            Accelerate(desiredVelocity);
                        }

                        public void Accelerate(double desiredVelocity) {
                            double velocityRemaining = desiredVelocity - Missile.Velocity;
                            bool isVelocityClose = Math.Abs(velocityRemaining) < 0.01;

                            if (isVelocityClose && Cruising) {
                                return;
                            }

                            if (isVelocityClose) {
                                Cruising = true;
                                ToggleThrusters(A_Thrusters, false);
                                ToggleThrusters(B_Thrusters, false);
                                return;
                            }

                            Cruising = false;
                            double newAcceleration = velocityRemaining / (decelerationInterval);
                            double force = TotalMissileMass * newAcceleration;
                            ApplyThrust(force);
                        }

                        private void ApplyThrust(double force) {
                            int thrustersLength = force < 0 ? B_Thrusters.Length : A_Thrusters.Length;
                            float thrust = (float)Math.Abs(force / thrustersLength);

                            if (force < 0) {
                                SetThrusterOverrides(A_Thrusters, 0.0f);
                                SetThrusterOverrides(B_Thrusters, thrust);
                            } else {
                                SetThrusterOverrides(B_Thrusters, 0.0f);
                                SetThrusterOverrides(A_Thrusters, thrust);
                            }
                        }
                    }

                    public static void ControlForward(double desiredVelocity) {
                        if (ForwardController == null) {
                            ForwardController = new VelocityController(V_buffer.Forward, N.Forward, ForwardThrusters, MaximumForwardThrust, ReverseThrusters, MaximumReverseThrust);
                        }
                        ForwardController.SetVelocity(desiredVelocity);
                    }

                    public static void ControlReverse(double desiredVelocity) {
                        if (ReverseController == null) {
                            ReverseController = new VelocityController(V_buffer.Reverse, N.Reverse, ReverseThrusters, MaximumReverseThrust, ForwardThrusters, MaximumForwardThrust);
                        }
                        ReverseController.SetVelocity(desiredVelocity);
                    }

                    public static void ControlLeft(double desiredVelocity) {
                        if (LeftController == null) {
                            LeftController = new VelocityController(V_buffer.Left, N.Left, LeftThrusters, MaximumLeftThrust, RightThrusters, MaximumRightThrust);
                        }
                        LeftController.SetVelocity(desiredVelocity);
                    }

                    public static void ControlRight(double desiredVelocity) {
                        if (RightController == null) {
                            RightController = new VelocityController(V_buffer.Right, N.Right, RightThrusters, MaximumRightThrust, LeftThrusters, MaximumLeftThrust);
                        }
                        RightController.SetVelocity(desiredVelocity);
                    }

                    public static void ControlUp(double desiredVelocity) {
                        if (UpController == null) {
                            UpController = new VelocityController(V_buffer.Up, N.Up, UpThrusters, MaximumUpThrust, DownThrusters, MaximumDownThrust);
                        }
                        UpController.SetVelocity(desiredVelocity);
                    }

                    public static void ControlDown(double desiredVelocity) {
                        if (DownController == null) {
                            DownController = new VelocityController(V_buffer.Down, N.Down, DownThrusters, MaximumDownThrust, UpThrusters, MaximumUpThrust);
                        }
                        DownController.SetVelocity(desiredVelocity);
                    }

                }

            }
        }
    }
}
