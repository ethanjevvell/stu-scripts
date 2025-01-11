using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public partial class STUFlightController {

            // Maneuver classes
            public HardStop HardStopManeuver { get; private set; }
            public GotoAndStop GotoAndStopManeuver { get; set; }
            public NavigateOverPlanetSurface NavigateOverPlanetSurfaceManeuver { get; set; }

            public static Queue<STULog> FlightLogs = new Queue<STULog>();
            private const string FLIGHT_CONTROLLER_LOG_NAME = "STU-FC";
            private const string FLIGHT_CONTROLLER_STANDARD_OUTPUT_TAG = "FLIGHT_CONTROLLER_STANDARD_OUTPUT";

            StandardOutput[] _standardOutputDisplays { get; set; }

            public IMyRemoteControl RemoteControl { get; protected set; }

            public bool HasGyroControl { get; private set; }
            public bool HasThrusterControl { get; private set; }

            public double TargetVelocity { get; protected set; }
            public double VelocityMagnitude { get; protected set; }
            public Vector3D CurrentVelocity_LocalFrame { get; protected set; }
            public Vector3D CurrentVelocity_WorldFrame { get; protected set; }
            public Vector3D AccelerationComponents { get; protected set; }

            public Vector3D CurrentPosition { get; protected set; }

            public MatrixD CurrentWorldMatrix { get; protected set; }
            public MatrixD PreviousWorldMatrix { get; protected set; }

            public IMyThrust[] ActiveThrusters { get; protected set; }
            public IMyGyro[] AllGyroscopes { get; protected set; }

            STUVelocityController _velocityController { get; set; }
            STUOrientationController _orientationController { get; set; }
            STUAltitudeController _altitudeController { get; set; }
            STUPointOrbitController _pointOrbitController { get; set; }
            STUPlanetOrbitController _planetOrbitController { get; set; }

            /// <summary>
            /// Flight utility class that handles velocity control and orientation control. Requires exactly one Remote Control block to function.
            /// Be sure to orient the Remote Control block so that its forward direction is the direction you want to be considered the "forward" direction of your ship.
            /// Also orient the Remote Control block so that its up direction is the direction you want to be considered the "up" direction of your ship.
            /// </summary>
            public STUFlightController(IMyGridTerminalSystem grid, IMyRemoteControl remoteControl, IMyTerminalBlock me) {
                RemoteControl = remoteControl;
                ActiveThrusters = FindThrusters(grid, me);
                AllGyroscopes = FindGyros(grid, me);
                TargetVelocity = 0;
                _velocityController = new STUVelocityController(RemoteControl, ActiveThrusters);
                _orientationController = new STUOrientationController(RemoteControl, AllGyroscopes);
                _altitudeController = new STUAltitudeController(this, RemoteControl);
                _pointOrbitController = new STUPointOrbitController(this, RemoteControl);
                _planetOrbitController = new STUPlanetOrbitController(this);
                HasGyroControl = true;
                _standardOutputDisplays = FindStandardOutputDisplays(grid, me);
                HardStopManeuver = new HardStop(this);
                UpdateState();
                CreateOkFlightLog("Flight controller initialized.");
            }

            public void UpdateThrustersAfterGridChange(IMyThrust[] newActiveThrusters) {
                _velocityController = new STUVelocityController(RemoteControl, newActiveThrusters);
                _altitudeController = new STUAltitudeController(this, RemoteControl);
            }

            public void MeasureCurrentVelocity() {
                CurrentVelocity_WorldFrame = RemoteControl.GetShipVelocities().LinearVelocity;
                CurrentVelocity_LocalFrame = STUTransformationUtils.WorldDirectionToLocalDirection(RemoteControl, CurrentVelocity_WorldFrame);
                VelocityMagnitude = CurrentVelocity_LocalFrame.Length();
            }

            public void MeasureCurrentAcceleration() {
                AccelerationComponents = _velocityController.CalculateAccelerationVectors();
            }

            public void MeasureCurrentPositionAndOrientation() {
                CurrentWorldMatrix = RemoteControl.WorldMatrix;
                CurrentPosition = RemoteControl.GetPosition();
            }

            public double GetCurrentSurfaceAltitude() {
                return _altitudeController.CurrentSurfaceAltitude;
            }

            public double GetCurrentSeaLevelAltitude() {
                return _altitudeController.CurrentSeaLevelAltitude;
            }

            public void ToggleThrusters(bool on) {
                foreach (var thruster in ActiveThrusters) {
                    thruster.Enabled = on;
                }
            }

            public float CalculateForwardStoppingDistance() {
                float mass = STUVelocityController.ShipMass;
                float velocity = (float)CurrentVelocity_LocalFrame.Z;
                float maxReverseAcceleration = _velocityController.GetMaximumReverseAcceleration();
                float dx = (velocity * velocity) / (2 * maxReverseAcceleration); // kinematic equation for "how far would I travel if I slammed on the brakes right now?"
                return dx;
            }

            public double CalculateStoppingDistance(double acceleration, double velocity) {
                double mass = STUVelocityController.ShipMass;
                double maxReverseAcceleration = acceleration;
                double dx = (velocity * velocity) / (2 * maxReverseAcceleration); // kinematic equation for "how far would I travel if I slammed on the brakes right now?"
                return dx;
            }

            /// <summary>
            /// Updates various aspects of the ship's state, including velocity, acceleration, position, and orientation.
            /// This must be called on every tick to ensure that the ship's state is up-to-date!
            /// </summary>
            public void UpdateState() {
                _velocityController.UpdateState();
                _altitudeController.UpdateState();
                MeasureCurrentPositionAndOrientation();
                MeasureCurrentVelocity();
                MeasureCurrentAcceleration();
                DrawToStandardOutputs();
            }

            private void DrawToStandardOutputs() {
                for (int i = 0; i < _standardOutputDisplays.Length; i++) {
                    _standardOutputDisplays[i].DrawTelemetry();
                }
            }

            /// <summary>
            /// Sets the ship's forward velocity. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetVx(double desiredVelocity) {
                return _velocityController.SetVx(CurrentVelocity_LocalFrame.X, desiredVelocity);
            }

            /// <summary>
            /// Sets the ship's rightward velocity. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetVy(double desiredVelocity) {
                return _velocityController.SetVy(CurrentVelocity_LocalFrame.Y, desiredVelocity);
            }

            /// <summary>
            /// Sets the ship's upward velocity. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetVz(double desiredVelocity) {
                return _velocityController.SetVz(CurrentVelocity_LocalFrame.Z, desiredVelocity);
            }

            /// <summary>
            /// Attempts to reach desiredVelocity in the world frame. Pass in a reference position to align the ship with that position. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="targetPos"></param>
            /// <param name="desiredVelocity"></param>
            /// <param name="referencePos"></param>
            /// <returns></returns>
            public bool SetV_WorldFrame(Vector3D targetPos, double desiredVelocity, Vector3D referencePos) {
                return _velocityController.SetV_WorldFrame(targetPos, CurrentVelocity_WorldFrame, referencePos, desiredVelocity);
            }

            /// <summary>
            /// Attempts to reach desiredVelocity in the world frame. Pass in a reference block to align that block with targetPos. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="targetPos"></param>
            /// <param name="desiredVelocity"></param>
            /// <param name="reference"></param>
            /// <returns></returns>
            public bool SetV_WorldFrame(Vector3D targetPos, double desiredVelocity, IMyTerminalBlock reference) {
                return _velocityController.SetV_WorldFrame(targetPos, CurrentVelocity_WorldFrame, reference.GetPosition(), desiredVelocity);
            }

            /// <summary>
            /// Attempts to reach desiredVelocity in the world frame. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="targetPos"></param>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetV_WorldFrame(Vector3D targetPos, double desiredVelocity) {
                return _velocityController.SetV_WorldFrame(targetPos, CurrentVelocity_WorldFrame, CurrentPosition, desiredVelocity);
            }

            /// <summary>
            /// Sets the ship's roll. Positive values roll the ship clockwise, negative values roll the ship counterclockwise.
            /// </summary>
            /// <param name="roll"></param>
            public void SetVr(double roll) {
                _orientationController.SetVr(roll);
            }

            public void Hover() {
                _velocityController.ExertVectorForce_WorldFrame(Vector3D.Zero, 0);
            }

            /// <summary>
            /// Sets the ship's pitch. Positive values pitch the ship clockwise, negative values pitch the ship counterclockwise. (probably)
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public void SetVp(double pitch) {
                _orientationController.SetVp(pitch);
            }

            /// <summary>
            /// Sets the ship's yaw. Positive values yaw the ship clockwise, negative values yaw the ship counterclockwise. (probably)
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public void SetVw(double yaw) {
                _orientationController.SetVw(yaw);
            }

            /// <summary>
            /// Sets the ship into a steady forward flight while controlling lateral thrusters. Good for turning while maintaining a forward velocity.
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetStableForwardVelocity(double desiredVelocity) {
                TargetVelocity = desiredVelocity;
                bool forwardStable = SetVz(desiredVelocity);
                bool rightStable = SetVx(0);
                bool upStable = SetVy(0);
                return forwardStable && rightStable && upStable;
            }

            /// <summary>
            /// Puts the ship into a stable free-fall by stabilizing x-y velocity components while letting z accelerate with gravity.
            /// </summary>
            /// <returns></returns>
            public bool StableFreeFall() {
                bool rightStable = SetVx(0);
                bool upStable = SetVy(0);
                return rightStable && upStable;
            }

            /// <summary>
            /// Aligns the ship's forward direction with the target position. Returns true if the ship is aligned.
            /// </summary>
            /// <param name="targetPos"></param>
            /// <returns></returns>
            public bool AlignShipToTarget(Vector3D targetPos, IMyTerminalBlock referenceBlock = null, string referenceBlockFace = null) {
                ReinstateGyroControl();
                return _orientationController.AlignShipToTarget(targetPos, referenceBlock, referenceBlockFace);
            }

            /// <summary>
            /// Rolls the ship to optimize the ship's inertia vector for a given target position. Effectively allows the ship to turn faster, at the cost of more fuel.
            /// </summary>
            /// <param name="targetPos"></param>
            public void OptimizeShipRoll(Vector3D targetPos) {
                Vector3D inertiaHeadingNormal = GetInertiaHeadingNormal(targetPos);
                if (inertiaHeadingNormal == Vector3D.Zero) { return; }

                // Get the current orientation of the ship
                MatrixD currentOrientation = CurrentWorldMatrix.GetOrientation();

                // Define the normals for two perpendicular lateral faces in the ship's local space
                Vector3D[] lateralFaceNormals = {
                    new Vector3D(0, 1, 0),  // Up
                    new Vector3D(1, 0, 0)   // Right
                };

                double smallestAngle = double.PositiveInfinity;
                double rollAdjustment = 0;

                // Find the lateral face normal with the smallest needed roll adjustment
                foreach (var lateralNormal in lateralFaceNormals) {
                    Vector3D worldNormal = Vector3D.TransformNormal(lateralNormal, currentOrientation);
                    double angle = CalculateRollAngle(inertiaHeadingNormal, worldNormal);

                    if (Math.Abs(angle) < Math.Abs(smallestAngle)) {
                        smallestAngle = angle;
                        rollAdjustment = smallestAngle;
                    }
                }

                // If close enough, stop the roll
                if (Math.Abs(rollAdjustment) < 0.003) {
                    _orientationController.SetVr(0);
                } else {
                    _orientationController.SetVr(rollAdjustment);
                }
            }

            /// <summary>
            /// Calculates a normal vector for the plane containing the ship's inertia vector and the vector from the ship to the target.
            /// </summary>
            /// <param name="targetPos"></param>
            /// <returns></returns>
            private Vector3D GetInertiaHeadingNormal(Vector3D targetPos) {
                // ship inertia vector
                Vector3D worldVelocity = Vector3D.Normalize(Vector3D.TransformNormal(CurrentVelocity_LocalFrame, CurrentWorldMatrix));
                // ship-to-target vector
                Vector3D ST = Vector3D.Normalize(targetPos - CurrentPosition);
                // normal vector of plane containing SI and ST
                Vector3D crossProduct = Vector3D.Cross(worldVelocity, ST);

                // Cross products approach zero as the two vectors approach parallel
                // In other words, the ship is moving directly towards the target, so no need to roll
                if (Math.Abs(crossProduct.Length()) < 0.01) {
                    return Vector3D.Zero;
                }

                return Vector3D.Normalize(crossProduct);
            }

            /// <summary>
            /// Calculate the roll angle needed to offset the ship's local lateral face normal 45 degrees from velocity-heading normal.
            /// </summary>
            /// <param name="normalVector"></param>
            /// <param name="lateralFaceNormal"></param>
            /// <returns></returns>
            private double CalculateRollAngle(Vector3D normalVector, Vector3D lateralFaceNormal) {
                var dotProduct = Vector3D.Dot(normalVector, lateralFaceNormal);
                var angle = Math.Acos(dotProduct);
                return angle - Math.PI / 4;
            }

            public Vector3D GetAltitudeVelocityChangeForceVector(double desiredVelocity, double altitudeVelocity) {
                // Note that LocalGravityVector has already been transformed by the ship's orientation
                Vector3D localGravityVector = _velocityController.LocalGravityVector;

                // Calculate the magnitude of the gravitational force
                double gravityForceMagnitude = localGravityVector.Length();

                if (gravityForceMagnitude == 0) {
                    return Vector3D.Zero;
                }

                // Total mass of the ship
                double mass = STUVelocityController.ShipMass;

                // Total force needed: F = ma; a acts as basic proportional controlller here
                double totalForceNeeded = mass * (gravityForceMagnitude + desiredVelocity - altitudeVelocity);

                // Normalize the gravity vector to get the direction
                Vector3D unitGravityVector = localGravityVector / gravityForceMagnitude;

                // Calculate the force vector needed (opposite to gravity and scaled by totalForceNeeded)
                Vector3D outputForce = -unitGravityVector * totalForceNeeded;

                return outputForce;
            }

            public void OrbitPoint(Vector3D targetPos) {
                _pointOrbitController.Run(targetPos);
            }

            public void OrbitPlanet() {
                _planetOrbitController.Run();
            }

            /// <summary>
            /// Sets the ship's altitude to the target altitude. Returns true if the ship's altitude is stable.
            /// Pass in desiredAltitudeVelocity if you want the ship to reach the target altitude at a certain speed.
            /// </summary>
            /// <param name="targetAltitude"></param>
            /// <param name="desiredAltitudeVelocity"></param>
            /// <returns></returns>
            public bool MaintainSurfaceAltitude(double targetAltitude, double ascendVelocity, double descendVelocity) {
                return _altitudeController.MaintainSurfaceAltitude(targetAltitude, ascendVelocity, descendVelocity);
            }

            /// <summary>
            /// Sets the ship's altitude to the target altitude. Returns true if the ship's altitude is stable.
            /// Pass in desiredAltitudeVelocity if you want the ship to reach the target altitude at a certain speed.
            /// </summary>
            /// <param name="targetAltitude"></param>
            /// <param name="desiredAltitudeVelocity"></param>
            /// <returns></returns>
            public bool MaintainSeaLevelAltitude(double targetAltitude, double ascendVelocity, double descendVelocity) {
                return _altitudeController.MaintainSeaLevelAltitude(targetAltitude, ascendVelocity, descendVelocity);
            }


            public void UpdateShipMass() {
                STUVelocityController.ShipMass = RemoteControl.CalculateShipMass().PhysicalMass;
            }

            public double GetShipMass() {
                return STUVelocityController.ShipMass;
            }

            public void RelinquishThrusterControl() {
                foreach (var thruster in ActiveThrusters) {
                    thruster.ThrustOverride = 0;
                }
                HasThrusterControl = false;
            }

            public void RelinquishGyroControl() {
                foreach (var gyro in AllGyroscopes) {
                    gyro.GyroOverride = false;
                }
                HasGyroControl = false;
            }

            public void ReinstateGyroControl() {
                foreach (var gyro in AllGyroscopes) {
                    gyro.GyroOverride = true;
                }
                HasGyroControl = true;
            }

            public void ReinstateThrusterControl() {
                foreach (var thruster in ActiveThrusters) {
                    thruster.ThrustOverride = 0;
                }
                HasThrusterControl = true;
            }

            private StandardOutput[] FindStandardOutputDisplays(IMyGridTerminalSystem grid, IMyTerminalBlock me) {
                List<StandardOutput> output = new List<StandardOutput>();
                List<IMyTerminalBlock> allBlocksOnGrid = new List<IMyTerminalBlock>();
                grid.GetBlocks(allBlocksOnGrid);
                allBlocksOnGrid.Where((block) => block.CubeGrid == me.CubeGrid);
                foreach (var block in allBlocksOnGrid) {
                    string customDataRawText = block.CustomData;
                    string[] customDataLines = customDataRawText.Split('\n');
                    foreach (var line in customDataLines) {
                        if (line.Contains(FLIGHT_CONTROLLER_STANDARD_OUTPUT_TAG)) {
                            string[] kvp = line.Split(':');
                            // adjust font size based on what screen we're trying to initalize
                            float fontSize;
                            try {
                                fontSize = float.Parse(kvp[2]);
                                if (fontSize < 0.1f || fontSize > 10f) {
                                    throw new Exception("Invalid font size");
                                }
                            } catch (Exception e) {
                                CreateWarningFlightLog("Invalid font size for display " + block.CustomName + ". Defaulting to 0.5");
                                CreateWarningFlightLog(e.Message);
                                fontSize = 0.5f;
                            }
                            StandardOutput screen = new StandardOutput(block, int.Parse(kvp[1]), "Monospace", fontSize);
                            output.Add(screen);
                        }
                    }
                }
                return output.ToArray();
            }

            public STUGalacticMap.Planet? GetPlanetOfPoint(Vector3D point) {
                foreach (var kvp in STUGalacticMap.CelestialBodies) {
                    STUGalacticMap.Planet planet = kvp.Value;
                    BoundingSphereD sphere = new BoundingSphereD(planet.Center, 1.76f * planet.Radius);
                    // if the point is inside the planet's detection sphere or intersects it, it is on the planet
                    if (sphere.Contains(point) == ContainmentType.Contains || sphere.Contains(point) == ContainmentType.Intersects) {
                        return planet;
                    }
                }
                return null;
            }

            public static void CreateOkFlightLog(string message) {
                CreateFlightLog(message, STULogType.OK);
            }

            public static void CreateErrorFlightLog(string message) {
                CreateFlightLog(message, STULogType.ERROR);
            }

            public static void CreateInfoFlightLog(string message) {
                CreateFlightLog(message, STULogType.INFO);
            }

            public static void CreateWarningFlightLog(string message) {
                CreateFlightLog(message, STULogType.WARNING);
            }

            public static void CreateFatalFlightLog(string message) {
                CreateFlightLog(message, STULogType.ERROR);
                throw new Exception(message);
            }

            private static void CreateFlightLog(string message, string type) {
                FlightLogs.Enqueue(new STULog {
                    Message = message,
                    Type = type,
                    Sender = FLIGHT_CONTROLLER_LOG_NAME
                });
            }

            IMyThrust[] FindThrusters(IMyGridTerminalSystem grid, IMyTerminalBlock me) {
                List<IMyTerminalBlock> thrusterBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyThrust>(thrusterBlocks, block => block.CubeGrid == me.CubeGrid);
                if (thrusterBlocks.Count == 0) {
                    CreateFatalFlightLog("No thrusters found on grid");
                }
                IMyThrust[] allThrusters = new IMyThrust[thrusterBlocks.Count];
                for (int i = 0; i < thrusterBlocks.Count; i++) {
                    allThrusters[i] = thrusterBlocks[i] as IMyThrust;
                }
                CreateOkFlightLog("Thrusters loaded successfully");
                return allThrusters;
            }

            IMyGyro[] FindGyros(IMyGridTerminalSystem grid, IMyTerminalBlock me) {
                List<IMyTerminalBlock> gyroBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGyro>(gyroBlocks, block => block.CubeGrid == me.CubeGrid);
                if (gyroBlocks.Count == 0) {
                    CreateFatalFlightLog("No gyros found on grid");
                }
                IMyGyro[] gyros = new IMyGyro[gyroBlocks.Count];
                for (int i = 0; i < gyroBlocks.Count; i++) {
                    gyros[i] = gyroBlocks[i] as IMyGyro;
                }
                CreateOkFlightLog("Gyros loaded successfully");
                return gyros;
            }

            public void ToggleDampeners(bool on) {
                RemoteControl.DampenersOverride = on;
            }

        }
    }
}
