using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
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
                public static float ShipMass { get; set; }

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
                public double MaximumReverseAcceleration { get; set; }
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

                public static Dictionary<string, double> ThrustCoefficients = new Dictionary<string, double>();

                private NTable NTable { get; set; }

                public STUVelocityController(IMyRemoteControl remoteControl, double timeStep, IMyThrust[] allThrusters, NTable Ntable = null) {

                    RemoteControl = remoteControl;

                    ShipMass = RemoteControl.CalculateShipMass().PhysicalMass;
                    dt = timeStep;
                    NTable = Ntable == null ? new NTable() : Ntable;

                    AssignThrustersByOrientation(allThrusters);
                    CalculateMaximumAccelerations();
                    CalculateBufferVelocities();
                    CalculateThrustCoefficients();

                    ForwardController = new VelocityController(ForwardBufferVelocity, NTable.Forward, ForwardThrusters, MaximumForwardThrust, ReverseThrusters, MaximumReverseThrust);
                    RightController = new VelocityController(RightBufferVelocity, NTable.Right, RightThrusters, MaximumRightThrust, LeftThrusters, MaximumLeftThrust);
                    UpController = new VelocityController(UpBufferVelocity, NTable.Up, UpThrusters, MaximumUpThrust, DownThrusters, MaximumDownThrust);
                }

                /// <summary>
                /// Velocity controller utility. Handles acceleration and deceleration automatically based on desired velocity; deceleration occurs with a roughly natural decay.
                /// </summary>
                private class VelocityController {

                    private const double VELOCITY_ERROR_TOLERANCE = 0.02;
                    private const double GRAVITY_ERROR_TOLERANCE = 0.02;

                    private bool ALREADY_COUNTERING_GRAVITY = false;

                    private double v_buffer;
                    private double N;
                    private double decelerationInterval;

                    private IMyThrust[] PosDirThrusters;
                    private IMyThrust[] NegDirThrusters;
                    private double MaxPosThrust;
                    private double MaxNegThrust;

                    public VelocityController(double buffer, double n, IMyThrust[] posDirThrusters, double maxPosThrust, IMyThrust[] negDirThrusters, double maxNegThrust) {
                        v_buffer = buffer;
                        N = n;
                        decelerationInterval = dt * N;
                        PosDirThrusters = posDirThrusters;
                        MaxPosThrust = maxPosThrust;
                        NegDirThrusters = negDirThrusters;
                        MaxNegThrust = maxNegThrust;
                    }

                    public bool SetVelocity(double currentVelocity, double desiredVelocity, double gravityVectorComponent) {

                        double remainingVelocityToGain = desiredVelocity - currentVelocity;

                        if (Math.Abs(remainingVelocityToGain) < VELOCITY_ERROR_TOLERANCE) {
                            CounteractGravity(gravityVectorComponent);
                            return true;
                        }

                        ALREADY_COUNTERING_GRAVITY = false;
                        return Accelerate(remainingVelocityToGain, gravityVectorComponent);
                    }

                    private void CounteractGravity(double gravityVectorComponent) {
                        // If we're operating on an axis that's fighting gravity, then we need to counteract gravity with acceleration of our own
                        if (Math.Abs(gravityVectorComponent) > GRAVITY_ERROR_TOLERANCE && !ALREADY_COUNTERING_GRAVITY) {
                            double counterForce = -gravityVectorComponent * ShipMass;
                            ALREADY_COUNTERING_GRAVITY = true;
                            ApplyThrust(counterForce);
                        }
                    }

                    // https://www.desmos.com/calculator/rsdijct8fq
                    public bool Accelerate(double remainingVelocityToGain, double gravityVectorComponent) {
                        double newAcceleration = (remainingVelocityToGain / decelerationInterval) - gravityVectorComponent;
                        double force = ShipMass * newAcceleration;
                        ApplyThrust(force);
                        return false;
                    }

                    private void ApplyThrust(double force) {
                        SetThrusterOverrides(PosDirThrusters, 0.0f);
                        SetThrusterOverrides(NegDirThrusters, 0.0f);
                        //int thrustersLength = force < 0 ? NegDirThrusters.Length : PosDirThrusters.Length;
                        if (force < 0) {
                            SetThrusterOverrides(NegDirThrusters, Math.Abs(force));
                        } else if (force > 0) {
                            SetThrusterOverrides(PosDirThrusters, Math.Abs(force));
                        }
                    }
                }

                public bool ControlVx(double currentVelocity, double desiredVelocity) {
                    var gravityVector = RemoteControl.GetNaturalGravity();
                    var localGravityVector = Math.Round(Vector3D.TransformNormal(gravityVector, MatrixD.Transpose(RemoteControl.WorldMatrix)).X, 2);
                    return RightController.SetVelocity(currentVelocity, desiredVelocity, localGravityVector);
                }

                public bool ControlVy(double currentVelocity, double desiredVelocity) {
                    var gravityVector = RemoteControl.GetNaturalGravity();
                    var localGravityVector = Math.Round(Vector3D.TransformNormal(gravityVector, MatrixD.Transpose(RemoteControl.WorldMatrix)).Y, 2);
                    return UpController.SetVelocity(currentVelocity, desiredVelocity, localGravityVector);
                }

                public bool ControlVz(double currentVelocity, double desiredVelocity) {
                    var gravityVector = RemoteControl.GetNaturalGravity();
                    var localGravityVector = Math.Round(Vector3D.TransformNormal(gravityVector, MatrixD.Transpose(RemoteControl.WorldMatrix)).Z, 2);
                    // Flip Gz to account for flipped forward-back orientation of Remote Control
                    return ForwardController.SetVelocity(currentVelocity, desiredVelocity, -localGravityVector);
                }

                private void AssignThrustersByOrientation(IMyThrust[] allThrusters) {

                    int forwardCount = 0;
                    int reverseCount = 0;
                    int leftCount = 0;
                    int rightCount = 0;
                    int upCount = 0;
                    int downCount = 0;

                    foreach (IMyThrust thruster in allThrusters) {

                        Base6Directions.Direction thrusterDirection = RemoteControl.Orientation.TransformDirectionInverse(thruster.Orientation.Forward);

                        // All thrusters have the opposite direction of what you'd expect
                        // I.e., a thruster facing forward will have a direction of backward, because a backward-facing thruster will push the ship forward
                        if (thrusterDirection == Base6Directions.Direction.Backward) {
                            forwardCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Forward) {
                            reverseCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Right) {
                            leftCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Left) {
                            rightCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Down) {
                            upCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Up) {
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

                        Base6Directions.Direction thrusterDirection = RemoteControl.Orientation.TransformDirectionInverse(thruster.Orientation.Forward);

                        if (thrusterDirection == Base6Directions.Direction.Backward) {
                            ForwardThrusters[forwardCount] = thruster;
                            MaximumForwardThrust += ForwardThrusters[forwardCount].MaxThrust;
                            forwardCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Forward) {
                            ReverseThrusters[reverseCount] = thruster;
                            MaximumReverseThrust += ReverseThrusters[reverseCount].MaxThrust;
                            reverseCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Right) {
                            LeftThrusters[leftCount] = thruster;
                            MaximumLeftThrust += LeftThrusters[leftCount].MaxThrust;
                            leftCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Left) {
                            RightThrusters[rightCount] = thruster;
                            MaximumRightThrust += RightThrusters[rightCount].MaxThrust;
                            rightCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Down) {
                            UpThrusters[upCount] = thruster;
                            MaximumUpThrust += UpThrusters[upCount].MaxThrust;
                            upCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Up) {
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

                private void CalculateThrustCoefficients() {
                    // Array of the the thruster groups
                    IMyThrust[][] thrusterGroups = new IMyThrust[][] { ForwardThrusters, ReverseThrusters, LeftThrusters, RightThrusters, UpThrusters, DownThrusters };
                    foreach (IMyThrust[] thrusterGroup in thrusterGroups) {
                        double maximumFaceThrust = 0;
                        foreach (IMyThrust thruster in thrusterGroup) {
                            maximumFaceThrust += thruster.MaxThrust;
                        }
                        foreach (IMyThrust thruster in thrusterGroup) {
                            double coefficient = thruster.MaxThrust / maximumFaceThrust;
                            ThrustCoefficients.Add(thruster.EntityId.ToString(), coefficient);
                        }
                    }
                }

                private static void SetThrusterOverrides(IMyThrust[] thrusters, double thrust) {
                    foreach (IMyThrust thruster in thrusters) {
                        // MaxEffectiveThrust = MaxThrust for hydrogen thrusters, but not for atmospheric or ion thrusters
                        // If we chose MaxThrust instead, hydrogen thrusters would be unaffected, but atmospheric and ion thrusters would always be inefficient fired at full power
                        double thrustCoefficient;
                        if (!ThrustCoefficients.TryGetValue(thruster.EntityId.ToString(), out thrustCoefficient)) {
                            throw new Exception("Thrust coefficient not found for thruster: " + thruster.EntityId);
                        }
                        thruster.ThrustOverride = Math.Min(thruster.MaxEffectiveThrust, (float)thrust * (float)thrustCoefficient);
                    }
                }

                private static void ToggleThrusters(IMyThrust[] thrusters, bool enabled) {
                    Array.ForEach(thrusters, thruster => thruster.Enabled = enabled);
                }

            }
        }
    }
}
