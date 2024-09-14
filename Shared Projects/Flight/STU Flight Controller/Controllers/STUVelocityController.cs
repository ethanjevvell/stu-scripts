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

                public bool FINISHED_ORIENTATION_CALCULATION = false;
                public float CALCULATION_PROGRESS = 0;

                IMyRemoteControl RemoteControl { get; set; }
                public Vector3D LocalGravityVector;

                IMyThrust[] HydrogenThrusters { get; set; }
                IMyThrust[] AtmosphericThrusters { get; set; }
                IMyThrust[] IonThrusters { get; set; }

                IMyThrust[] ForwardThrusters { get; set; }
                IMyThrust[] ReverseThrusters { get; set; }
                IMyThrust[] LeftThrusters { get; set; }
                IMyThrust[] RightThrusters { get; set; }
                IMyThrust[] UpThrusters { get; set; }
                IMyThrust[] DownThrusters { get; set; }

                VelocityController ForwardController { get; set; }
                VelocityController RightController { get; set; }
                VelocityController UpController { get; set; }

                public Vector3D MaximumThrustVector { get; set; }
                public Vector3D MinimumThrustVector { get; set; }
                public IMyThrust[] MaximumThrustVectorThrusters { get; set; }

                public static Dictionary<string, double> ThrustCoefficients = new Dictionary<string, double>();

                public STUVelocityController(IMyRemoteControl remoteControl, IMyThrust[] allThrusters) {

                    ThrustCoefficients.Clear();
                    RemoteControl = remoteControl;

                    ShipMass = RemoteControl.CalculateShipMass().PhysicalMass;
                    LocalGravityVector = RemoteControl.GetNaturalGravity();

                    AssignThrustersByOrientation(allThrusters);
                    CalculateThrustCoefficients();

                    ForwardController = new VelocityController(ForwardThrusters, ReverseThrusters);
                    RightController = new VelocityController(RightThrusters, LeftThrusters);
                    UpController = new VelocityController(UpThrusters, DownThrusters);

                    MaximumThrustVector = GetMaximumThrustVector();
                    MinimumThrustVector = GetMinimumThrustVector();

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

                    public void ApplyThrust(double force) {
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
                    return ForwardController.SetVelocity(currentVelocity, desiredVelocity, LocalGravityVector.Z);
                }


                public void SetFx(double force) {
                    RightController.ApplyThrust(force);
                }

                public void SetFy(double force) {
                    UpController.ApplyThrust(force);
                }

                public void SetFz(double force) {
                    ForwardController.ApplyThrust(force);
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
                    // flip Z to account for flipped forward-back orientation of Remote Control
                    LocalGravityVector.Z = -Math.Round(LocalGravityVector.Z, 2);
                }

                private void AssignThrustersByOrientation(IMyThrust[] allThrusters) {

                    int forwardCount = 0;
                    int reverseCount = 0;
                    int leftCount = 0;
                    int rightCount = 0;
                    int upCount = 0;
                    int downCount = 0;

                    int hydrogenCount = 0;
                    int atmosphericCount = 0;
                    int ionCount = 0;

                    foreach (IMyThrust thruster in allThrusters) {

                        Base6Directions.Direction thrusterDirection = RemoteControl.Orientation.TransformDirectionInverse(thruster.Orientation.Forward);
                        string thrusterType = GetThrusterType(thruster);

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

                        if (thrusterType == "Hydrogen") {
                            hydrogenCount++;
                        }

                        if (thrusterType == "Atmospheric") {
                            atmosphericCount++;
                        }

                        if (thrusterType == "Ion") {
                            ionCount++;
                        }

                    }

                    ForwardThrusters = new IMyThrust[forwardCount];
                    ReverseThrusters = new IMyThrust[reverseCount];
                    LeftThrusters = new IMyThrust[leftCount];
                    RightThrusters = new IMyThrust[rightCount];
                    UpThrusters = new IMyThrust[upCount];
                    DownThrusters = new IMyThrust[downCount];

                    HydrogenThrusters = new IMyThrust[hydrogenCount];
                    AtmosphericThrusters = new IMyThrust[atmosphericCount];
                    IonThrusters = new IMyThrust[ionCount];

                    forwardCount = 0;
                    reverseCount = 0;
                    leftCount = 0;
                    rightCount = 0;
                    upCount = 0;
                    downCount = 0;

                    hydrogenCount = 0;
                    atmosphericCount = 0;
                    ionCount = 0;

                    foreach (IMyThrust thruster in allThrusters) {

                        Base6Directions.Direction thrusterDirection = RemoteControl.Orientation.TransformDirectionInverse(thruster.Orientation.Forward);
                        string thrusterType = GetThrusterType(thruster);

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

                        if (thrusterType == "Hydrogen") {
                            HydrogenThrusters[hydrogenCount] = thruster;
                            hydrogenCount++;
                        }

                        if (thrusterType == "Atmospheric") {
                            AtmosphericThrusters[atmosphericCount] = thruster;
                            atmosphericCount++;
                        }

                        if (thrusterType == "Ion") {
                            IonThrusters[ionCount] = thruster;
                            ionCount++;
                        }

                    }

                }

                public Vector3D CalculateAccelerationVectors() {
                    Vector3D accelerationVecor;
                    accelerationVecor.X = CalculateNetThrust(RightThrusters, LeftThrusters) / ShipMass + LocalGravityVector.X;
                    accelerationVecor.Y = CalculateNetThrust(UpThrusters, DownThrusters) / ShipMass + LocalGravityVector.Y;
                    accelerationVecor.Z = CalculateNetThrust(ForwardThrusters, ReverseThrusters) / ShipMass + LocalGravityVector.Z;
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

                private string GetThrusterType(IMyThrust thruster) {
                    string thrusterName = thruster.DefinitionDisplayNameText;
                    if (thrusterName.Contains("Hydrogen")) {
                        return "Hydrogen";
                    } else if (thrusterName.Contains("Atmospheric")) {
                        return "Atmospheric";
                    } else if (thrusterName.Contains("Ion")) {
                        return "Ion";
                    } else {
                        throw new Exception("Unknown thruster type: " + thruster.DefinitionDisplayNameText);
                    }
                }

                public string GetThrusterTypes() {
                    return $"Hydrogen: {HydrogenThrusters.Length}, Atmospheric: {AtmosphericThrusters.Length}, Ion: {IonThrusters.Length}";
                }

                /// <summary>
                /// Finds the maximum thrust vector for the ship by iterating through all possible orientations and finding the one with the maximum total thrust.
                /// Has the side effect of setting the MaximumThrustVectorThrusters property to the thrusters used in the maximum thrust orientation.
                /// </summary>
                /// <returns></returns>
                private Vector3D GetMaximumThrustVector() {
                    // Calculate total thrust in positive and negative directions along each axis
                    float T_f = ForwardThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_b = ReverseThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_r = RightThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_l = LeftThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_u = UpThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_d = DownThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);

                    // Define the eight possible octants (corners of the cube) using integer arrays
                    var octants = new List<int[]> {
                        new int[] { 1,  1,  1 },
                        new int[] { 1,  1, -1 },
                        new int[] { 1, -1,  1 },
                        new int[] { 1, -1, -1 },
                        new int[] { -1,  1,  1 },
                        new int[] { -1,  1, -1 },
                        new int[] { -1, -1,  1 },
                        new int[] { -1, -1, -1 }
                    };

                    float maxTotalThrust = float.MinValue;
                    Vector3D maxThrustVector = Vector3D.Zero;

                    // Initialize the thruster arrays for the maximum thrust orientation
                    IMyThrust[] maxThrustersX = null;
                    IMyThrust[] maxThrustersY = null;
                    IMyThrust[] maxThrustersZ = null;

                    // Iterate through each octant to find the one with the maximum total thrust
                    foreach (var octant in octants) {
                        int signX = octant[0];
                        int signY = octant[1];
                        int signZ = octant[2];

                        float thrust_x = (signX == 1) ? T_r : T_l;
                        float thrust_y = (signY == 1) ? T_u : T_d;
                        float thrust_z = (signZ == 1) ? T_f : T_b;
                        float totalThrust = thrust_x + thrust_y + thrust_z;

                        if (totalThrust > maxTotalThrust) {
                            maxTotalThrust = totalThrust;
                            maxThrustVector = new Vector3D(
                                signX * thrust_x,
                                signY * thrust_y,
                                // Flip Z to account for flipped forward-back orientation of Remote Control
                                signZ * thrust_z * -1
                            );

                            // Store the thrusters used in this orientation
                            maxThrustersX = (signX == 1) ? RightThrusters : LeftThrusters;
                            maxThrustersY = (signY == 1) ? UpThrusters : DownThrusters;
                            maxThrustersZ = (signZ == 1) ? ForwardThrusters : ReverseThrusters;
                        }
                    }

                    // Combine the thrusters for the maximum thrust orientation
                    MaximumThrustVectorThrusters = maxThrustersX.Concat(maxThrustersY).Concat(maxThrustersZ).ToArray();
                    return maxThrustVector;

                }

                /// <summary>
                /// Finds the minimum thrust vector for the ship by iterating through all possible orientations and finding the one with the minimum total thrust.
                /// This is used by AC130 mode to find the orientation where the ship would provide the least centriptal force.
                /// </summary>
                /// <returns></returns>
                private Vector3D GetMinimumThrustVector() {

                    // Calculate total thrust in positive and negative directions along each axis
                    float T_f = ForwardThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_b = ReverseThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_r = RightThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_l = LeftThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_u = UpThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_d = DownThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);

                    var sextants = new List<int[]> {
                        new int[] {0, 0, 1},
                        new int[] {0, 0, -1},
                        new int[] {0, 1, 0},
                        new int[] {0, -1, 0},
                        new int[] {1, 0, 0},
                        new int[] {-1, 0, 0}
                    };

                    float minThrust = float.MaxValue;
                    Vector3D minThrustVector = Vector3D.Zero;

                    IMyThrust[] minThrustersX = null;
                    IMyThrust[] minThrustersY = null;
                    IMyThrust[] minThrustersZ = null;

                    foreach (var sextant in sextants) {
                        int signX = sextant[0];
                        int signY = sextant[1];
                        int signZ = sextant[2];

                        float thrust_x = (signX == 1) ? T_r : (signX == -1) ? T_l : 0;
                        float thrust_y = (signY == 1) ? T_u : (signY == -1) ? T_d : 0;
                        float thrust_z = (signZ == 1) ? T_f : (signZ == -1) ? T_b : 0;

                        float totalThrust = thrust_x + thrust_y + thrust_z;

                        if (totalThrust < minThrust) {
                            minThrust = totalThrust;
                            minThrustVector = new Vector3D(
                                signX * thrust_x,
                                signY * thrust_y,
                                signZ * thrust_z * -1
                            );

                            // Store the thrusters used in this orientation
                            minThrustersX = (signX == 1) ? RightThrusters : (signX == -1) ? LeftThrusters : new IMyThrust[] { };
                            minThrustersY = (signY == 1) ? UpThrusters : (signY == -1) ? DownThrusters : new IMyThrust[] { };
                            minThrustersZ = (signZ == 1) ? ForwardThrusters : (signZ == -1) ? ReverseThrusters : new IMyThrust[] { };

                        }
                    }

                    CreateInfoFlightLog($"Minimum thrust vector: {minThrustVector}");
                    return minThrustVector;

                }
            }

        }
    }
}
