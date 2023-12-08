using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            /// <summary>
            /// A coefficient for the dt between each script run. `N` should be at the very minimum greater than 1.
            /// Higher values of `N` will result in smoother deceleration, but LIGMA will cover more ground before achieving the desired velocity.
            /// Lower values of `N` will result in more abrupt deceleration.
            /// </summary>
            public class NTable {

                public double Forward = 6;
                public double Reverse = 6;
                public double Left = 6;
                public double Right = 6;
                public double Up = 6;
                public double Down = 6;

                public NTable() { }

            }

            public partial class STUVelocityController {

                /// <summary>
                /// Timestep shared by all velocity controllers; it's impossible to have multiple timesteps in a single script.
                /// </summary>
                static double dt { get; set; }
                /// <summary>
                /// All velocity controllers deal with the same grid mass, so it's shared.
                /// </summary>
                static float ShipMass { get; set; }

                IMyRemoteControl RemoteControl { get; set; }

                IMyThrust[] ForwardThrusters { get; set; }
                IMyThrust[] ReverseThrusters { get; set; }
                IMyThrust[] LeftThrusters { get; set; }
                IMyThrust[] RightThrusters { get; set; }
                IMyThrust[] UpThrusters { get; set; }
                IMyThrust[] DownThrusters { get; set; }

                double MaximumForwardThrust { get; set; }
                double MaximumReverseThrust { get; set; }
                double MaximumLeftThrust { get; set; }
                double MaximumRightThrust { get; set; }
                double MaximumUpThrust { get; set; }
                double MaximumDownThrust { get; set; }

                double MaximumForwardAcceleration { get; set; }
                double MaximumReverseAcceleration { get; set; }
                double MaximumLeftAcceleration { get; set; }
                double MaximumRightAcceleration { get; set; }
                double MaximumUpAcceleration { get; set; }
                double MaximumDownAcceleration { get; set; }

                double ForwardBufferVelocity { get; set; }
                double ReverseBufferVelocity { get; set; }
                double LeftBufferVelocity { get; set; }
                double RightBufferVelocity { get; set; }
                double UpBufferVelocity { get; set; }
                double DownBufferVelocity { get; set; }

                VelocityController ForwardController { get; set; }
                VelocityController RightController { get; set; }
                VelocityController UpController { get; set; }

                private NTable NTable { get; set; }

                public STUVelocityController(IMyRemoteControl remoteControl, double timeStep, IMyThrust[] allThrusters, float shipMass, NTable Ntable = null) {

                    RemoteControl = remoteControl;

                    ShipMass = shipMass;
                    dt = timeStep;
                    NTable = Ntable == null ? new NTable() : Ntable;

                    AssignThrustersByOrientation(allThrusters);
                    CalculateMaximumAccelerations();
                    CalculateBufferVelocities();

                    ForwardController = new VelocityController(ForwardBufferVelocity, NTable.Forward, ForwardThrusters, MaximumForwardThrust, ReverseThrusters, MaximumReverseThrust);
                    RightController = new VelocityController(RightBufferVelocity, NTable.Right, RightThrusters, MaximumRightThrust, LeftThrusters, MaximumLeftThrust);
                    UpController = new VelocityController(UpBufferVelocity, NTable.Up, UpThrusters, MaximumUpThrust, DownThrusters, MaximumDownThrust);

                }

                /// <summary>
                /// Velocity controller utility. Handles acceleration and deceleration automatically based on desired velocity; deceleration occurs with a roughly natural decay.
                /// </summary>
                private class VelocityController {

                    private double v_buffer;
                    private double N;
                    private double decelerationInterval;

                    private IMyThrust[] A_Thrusters;
                    private IMyThrust[] B_Thrusters;
                    private double MaximumAThrust;
                    private double MaximumBThrust;

                    public bool Cruising = false;
                    public bool AtMaxAcceleration = false;

                    public VelocityController(double buffer, double n, IMyThrust[] aThrusters, double maxAThrust, IMyThrust[] bThrusters, double maxBThrust) {
                        v_buffer = buffer;
                        N = n;
                        decelerationInterval = dt * N;
                        A_Thrusters = aThrusters;
                        MaximumAThrust = maxAThrust;
                        B_Thrusters = bThrusters;
                        MaximumBThrust = maxBThrust;
                    }

                    public void SetVelocity(double v, double desiredVelocity) {
                        double velocityDiff = desiredVelocity - v;

                        if (!Cruising) {
                            ToggleThrusters(A_Thrusters, true);
                            ToggleThrusters(B_Thrusters, true);
                        }

                        if (velocityDiff > v_buffer) {
                            if (!AtMaxAcceleration) {
                                SetThrusterOverrides(A_Thrusters, MaximumAThrust);
                                AtMaxAcceleration = true;
                                Cruising = false;
                            }
                            return;
                        }

                        AtMaxAcceleration = false;
                        Accelerate(v, desiredVelocity);
                    }

                    public void Accelerate(double v, double desiredVelocity) {
                        double velocityRemaining = desiredVelocity - v;
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
                        double force = ShipMass * newAcceleration;
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

                public bool ControlForward(double currentVelocity, double desiredVelocity) {
                    ForwardController.SetVelocity(currentVelocity, desiredVelocity);
                    return ForwardController.Cruising;
                }

                public bool ControlRight(double currentVelocity, double desiredVelocity) {
                    RightController.SetVelocity(currentVelocity, desiredVelocity);
                    return RightController.Cruising;
                }

                public bool ControlUp(double currentVelocity, double desiredVelocity) {
                    UpController.SetVelocity(currentVelocity, desiredVelocity);
                    return UpController.Cruising;
                }

                /// <summary>
                /// Resets the internal state of all velocity controllers.
                /// Use this after finishing using controllers if you're getting unexpected behaviour.
                /// </summary>
                public void ResetControllers() {
                    ForwardController.Cruising = false;
                    RightController.Cruising = false;
                    UpController.Cruising = false;
                }

                private void AssignThrustersByOrientation(IMyThrust[] allThrusters) {

                    int forwardCount = 0;
                    int reverseCount = 0;
                    int leftCount = 0;
                    int rightCount = 0;
                    int upCount = 0;
                    int downCount = 0;

                    foreach (IMyThrust thruster in allThrusters) {

                        MyBlockOrientation thrusterDirection = thruster.Orientation;

                        if (thrusterDirection.Forward == Base6Directions.Direction.Forward) {
                            forwardCount++;
                        }

                        if (thrusterDirection.Forward == Base6Directions.Direction.Backward) {
                            reverseCount++;
                        }

                        // in-game geometry is the reverse of what you'd expect for left-right
                        if (thrusterDirection.Forward == Base6Directions.Direction.Right) {
                            leftCount++;
                        }

                        // in-game geometry is the reverse of what you'd expect for left-right
                        if (thrusterDirection.Forward == Base6Directions.Direction.Left) {
                            rightCount++;
                        }

                        if (thrusterDirection.Forward == Base6Directions.Direction.Up) {
                            upCount++;
                        }

                        if (thrusterDirection.Forward == Base6Directions.Direction.Down) {
                            downCount++;
                        }

                    }

                    ForwardThrusters = new IMyThrust[forwardCount];
                    ReverseThrusters = new IMyThrust[reverseCount];
                    LeftThrusters = new IMyThrust[leftCount];
                    RightThrusters = new IMyThrust[rightCount];
                    UpThrusters = new IMyThrust[upCount];
                    DownThrusters = new IMyThrust[downCount];

                    forwardCount = 0;
                    reverseCount = 0;
                    leftCount = 0;
                    rightCount = 0;
                    upCount = 0;
                    downCount = 0;

                    foreach (IMyThrust thruster in allThrusters) {

                        MyBlockOrientation thrusterDirection = thruster.Orientation;

                        if (thrusterDirection.Forward == Base6Directions.Direction.Forward) {
                            ForwardThrusters[forwardCount] = thruster;
                            MaximumForwardThrust += ForwardThrusters[forwardCount].MaxThrust;
                            forwardCount++;
                        }

                        if (thrusterDirection.Forward == Base6Directions.Direction.Backward) {
                            ReverseThrusters[reverseCount] = thruster;
                            MaximumReverseThrust += ReverseThrusters[reverseCount].MaxThrust;
                            reverseCount++;
                        }

                        // in-game geometry is the reverse of what you'd expect for left-right
                        if (thrusterDirection.Forward == Base6Directions.Direction.Right) {
                            LeftThrusters[leftCount] = thruster;
                            MaximumLeftThrust += LeftThrusters[leftCount].MaxThrust;
                            leftCount++;
                        }

                        // in-game geometry is the reverse of what you'd expect for left-right
                        if (thrusterDirection.Forward == Base6Directions.Direction.Left) {
                            RightThrusters[rightCount] = thruster;
                            MaximumRightThrust += RightThrusters[rightCount].MaxThrust;
                            rightCount++;
                        }

                        if (thrusterDirection.Forward == Base6Directions.Direction.Up) {
                            UpThrusters[upCount] = thruster;
                            MaximumUpThrust += UpThrusters[upCount].MaxThrust;
                            upCount++;
                        }

                        if (thrusterDirection.Forward == Base6Directions.Direction.Down) {
                            DownThrusters[downCount] = thruster;
                            MaximumDownThrust += DownThrusters[downCount].MaxThrust;
                            downCount++;
                        }

                    }

                }

                private void CalculateMaximumAccelerations() {
                    MaximumForwardAcceleration = MaximumForwardThrust / ShipMass;
                    MaximumReverseAcceleration = MaximumReverseThrust / ShipMass;
                    MaximumLeftAcceleration = MaximumLeftThrust / ShipMass;
                    MaximumRightAcceleration = MaximumRightThrust / ShipMass;
                    MaximumUpAcceleration = MaximumUpThrust / ShipMass;
                    MaximumDownAcceleration = MaximumDownThrust / ShipMass;
                }

                private void CalculateBufferVelocities() {
                    ForwardBufferVelocity = MaximumForwardAcceleration * dt * NTable.Forward;
                    ReverseBufferVelocity = MaximumReverseAcceleration * dt * NTable.Reverse;
                    LeftBufferVelocity = MaximumLeftAcceleration * dt * NTable.Left;
                    RightBufferVelocity = MaximumRightAcceleration * dt * NTable.Right;
                    UpBufferVelocity = MaximumUpAcceleration * dt * NTable.Up;
                    DownBufferVelocity = MaximumDownAcceleration * dt * NTable.Down;
                }

                private static void SetThrusterOverrides(IMyThrust[] thrusters, double overrideValue) {
                    // Thrust override is capped at the max effective thrust
                    Array.ForEach(thrusters, thruster => thruster.ThrustOverride = Math.Min(thruster.MaxEffectiveThrust, (float)overrideValue));
                }

                private static void ToggleThrusters(IMyThrust[] thrusters, bool enabled) {
                    Array.ForEach(thrusters, thruster => thruster.Enabled = enabled);
                }

            }
        }
    }
}
