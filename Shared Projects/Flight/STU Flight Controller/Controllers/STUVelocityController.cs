using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            public partial class STUVelocityController {

                /// <summary>
                /// All velocity controllers deal with the same grid mass, so it's shared.
                /// </summary>
                public static float ShipMass { get; set; }

                IMyRemoteControl RemoteControl { get; set; }
                Vector3D LocalGravityVector;

                IMyThrust[] ForwardThrusters { get; set; }
                IMyThrust[] ReverseThrusters { get; set; }
                IMyThrust[] LeftThrusters { get; set; }
                IMyThrust[] RightThrusters { get; set; }
                IMyThrust[] UpThrusters { get; set; }
                IMyThrust[] DownThrusters { get; set; }

                VelocityController ForwardController { get; set; }
                VelocityController RightController { get; set; }
                VelocityController UpController { get; set; }

                public static Dictionary<string, double> ThrustCoefficients = new Dictionary<string, double>();

                public STUVelocityController(IMyRemoteControl remoteControl, IMyThrust[] allThrusters) {

                    RemoteControl = remoteControl;

                    ShipMass = RemoteControl.CalculateShipMass().PhysicalMass;
                    LocalGravityVector = RemoteControl.GetNaturalGravity();

                    AssignThrustersByOrientation(allThrusters);
                    CalculateThrustCoefficients();

                    ForwardController = new VelocityController(ForwardThrusters, ReverseThrusters);
                    RightController = new VelocityController(RightThrusters, LeftThrusters);
                    UpController = new VelocityController(UpThrusters, DownThrusters);

                }

                /// <summary>
                /// Velocity controller utility. Handles acceleration and deceleration automatically based on desired velocity; deceleration occurs with a roughly natural decay.
                /// </summary>
                private class VelocityController {

                    // Tolerance for velocity error; if the error is less than this, we're done
                    private const double VELOCITY_ERROR_TOLERANCE = 0.02;
                    // Extremely small gravity levels are neglible; if the gravity vector component is less than this, we don't need to counteract it,
                    // otherwise we're just wasting computation
                    private const double GRAVITY_ERROR_TOLERANCE = 0.02;

                    private bool ALREADY_COUNTERING_GRAVITY = false;

                    private IMyThrust[] PosDirThrusters;
                    private IMyThrust[] NegDirThrusters;

                    public VelocityController(IMyThrust[] posDirThrusters, IMyThrust[] negDirThrusters) {
                        PosDirThrusters = posDirThrusters;
                        NegDirThrusters = negDirThrusters;
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
                        double newAcceleration = remainingVelocityToGain - gravityVectorComponent;
                        double force = ShipMass * newAcceleration;
                        ApplyThrust(force);
                        return false;
                    }

                    private void ApplyThrust(double force) {
                        SetThrusterOverrides(PosDirThrusters, 0.0f);
                        SetThrusterOverrides(NegDirThrusters, 0.0f);
                        if (force < 0) {
                            SetThrusterOverrides(NegDirThrusters, Math.Abs(force));
                        } else {
                            SetThrusterOverrides(PosDirThrusters, Math.Abs(force));
                        }
                    }
                }

                public bool SetVx(double currentVelocity, double desiredVelocity) {
                    return RightController.SetVelocity(currentVelocity, desiredVelocity, LocalGravityVector.X);
                }

                public bool SetVy(double currentVelocity, double desiredVelocity) {
                    return UpController.SetVelocity(currentVelocity, desiredVelocity, LocalGravityVector.Y);
                }

                public bool SetVz(double currentVelocity, double desiredVelocity) {
                    // Flip Gz to account for flipped forward-back orientation of Remote Control
                    return ForwardController.SetVelocity(currentVelocity, desiredVelocity, -LocalGravityVector.Z);
                }

                public float GetMaximumReverseAcceleration() {
                    return ReverseThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxEffectiveThrust) / ShipMass;
                }

                /// <summary>
                /// Updates state variables relevant to the velocity controller. For now, just the local gravity vector.
                /// </summary>
                public void UpdateState() {
                    var localGravity = RemoteControl.GetNaturalGravity();
                    LocalGravityVector = Vector3D.TransformNormal(localGravity, MatrixD.Transpose(RemoteControl.WorldMatrix));
                    LocalGravityVector.X = Math.Round(LocalGravityVector.X, 2);
                    LocalGravityVector.Y = Math.Round(LocalGravityVector.Y, 2);
                    LocalGravityVector.Z = Math.Round(LocalGravityVector.Z, 2);
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
                            forwardCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Forward) {
                            ReverseThrusters[reverseCount] = thruster;
                            reverseCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Right) {
                            LeftThrusters[leftCount] = thruster;
                            leftCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Left) {
                            RightThrusters[rightCount] = thruster;
                            rightCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Down) {
                            UpThrusters[upCount] = thruster;
                            upCount++;
                        }

                        if (thrusterDirection == Base6Directions.Direction.Up) {
                            DownThrusters[downCount] = thruster;
                            downCount++;
                        }

                    }

                }

                public Vector3D CalculateAccelerationVectors() {
                    Vector3D accelerationVecor;
                    accelerationVecor.X = CalculateNetThrust(RightThrusters, LeftThrusters) / ShipMass + LocalGravityVector.X;
                    accelerationVecor.Y = CalculateNetThrust(UpThrusters, DownThrusters) / ShipMass + LocalGravityVector.Y;
                    accelerationVecor.Z = CalculateNetThrust(ForwardThrusters, ReverseThrusters) / ShipMass - LocalGravityVector.Z;
                    return accelerationVecor;
                }

                private float CalculateNetThrust(IMyThrust[] posDirThrusters, IMyThrust[] negDirThrusters) {
                    float posThrust = 0;
                    float negThrust = 0;
                    foreach (IMyThrust thrust in posDirThrusters) {
                        posThrust += thrust.ThrustOverride;
                    }
                    foreach (IMyThrust thrust in negDirThrusters) {
                        negThrust += thrust.ThrustOverride;
                    }
                    return posThrust - negThrust;
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

            }
        }
    }
}
