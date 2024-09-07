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

                    MaximumThrustVector = GetMaximumThrustOrientation();

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

                private Vector3D GetMaximumThrustOrientation() {

                    float T_f = ForwardThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_b = ReverseThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_r = RightThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_l = LeftThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_u = UpThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);
                    float T_d = DownThrusters.Aggregate(0.0f, (acc, thruster) => acc + thruster.MaxThrust);

                    // Unit vectors for each face (assuming initial alignment with axes)
                    var vectors = new Dictionary<string, Vector3>
                    {
            { "Front", new Vector3(T_f, 0, 0) },
            { "Back", new Vector3(-T_b, 0, 0) },
            { "Left", new Vector3(0, T_l, 0) },
            { "Right", new Vector3(0, -T_r, 0) },
            { "Top", new Vector3(0, 0, T_u) },
            { "Bottom", new Vector3(0, 0, -T_d) }
        };

                    // Get side with most thrust
                    var sideWithMostThrust = vectors.Aggregate((x, y) => x.Value.Length() > y.Value.Length() ? x : y).Key;
                    var sideWithMostThrustIndex = vectors.Keys.ToList().IndexOf(sideWithMostThrust);

                    // Get sides adjacent to side with most thrust
                    var adjacentSidesMap = new Dictionary<string, List<string>>
                    {
            { "Front", new List<string> { "Left", "Right", "Top", "Bottom" } },
            { "Back", new List<string> { "Left", "Right", "Top", "Bottom" } },
            { "Left", new List<string> { "Front", "Back", "Top", "Bottom" } },
            { "Right", new List<string> { "Front", "Back", "Top", "Bottom" } },
            { "Top", new List<string> { "Front", "Back", "Left", "Right" } },
            { "Bottom", new List<string> { "Front", "Back", "Left", "Right" } }
        };

                    var adjacentSides = adjacentSidesMap[sideWithMostThrust];

                    // Get adjacent side with most thrust
                    var sideWithMostThrustAdjacent = adjacentSides.Aggregate((x, y) => vectors[x].Length() > vectors[y].Length() ? x : y);
                    var sideWithMostThrustAdjacentIndex = vectors.Keys.ToList().IndexOf(sideWithMostThrustAdjacent);

                    // Get sides to avoid
                    var sidesToAvoid = new List<string> { sideWithMostThrust, sideWithMostThrustAdjacent };
                    var sidesToAvoidIndices = sidesToAvoid.Select(side => vectors.Keys.ToList().IndexOf(side)).ToList();

                    // Get the side opposite of the side with the most thrust
                    var oppositeSideMap = new Dictionary<string, string>
                    {
            { "Front", "Back" },
            { "Back", "Front" },
            { "Left", "Right" },
            { "Right", "Left" },
            { "Top", "Bottom" },
            { "Bottom", "Top" }
        };

                    var oppositeSide = oppositeSideMap[sideWithMostThrust];
                    var oppositeSideIndex = vectors.Keys.ToList().IndexOf(oppositeSide);

                    var secondOppositeSide = oppositeSideMap[sideWithMostThrustAdjacent];
                    var secondOppositeSideIndex = vectors.Keys.ToList().IndexOf(secondOppositeSide);

                    sidesToAvoidIndices.Add(oppositeSideIndex);
                    sidesToAvoidIndices.Add(secondOppositeSideIndex);

                    // Find remaining adjacent sides
                    var remainingSidesIndices = Enumerable.Range(0, 6).Where(i => !sidesToAvoidIndices.Contains(i)).ToList();

                    // Get the index of the remaining adjacent side with the most thrust
                    var sideWithMostThrustRemainingIndex = remainingSidesIndices.Aggregate((x, y) => vectors[vectors.Keys.ElementAt(x)].Length() > vectors[vectors.Keys.ElementAt(y)].Length() ? x : y);

                    return vectors[sideWithMostThrust] + vectors[sideWithMostThrustAdjacent] + vectors[vectors.Keys.ElementAt(sideWithMostThrustRemainingIndex)];

                }


            }

        }
    }
}
