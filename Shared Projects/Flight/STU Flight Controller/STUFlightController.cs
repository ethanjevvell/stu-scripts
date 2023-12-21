using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public partial class STUFlightController {

            float TimeStep { get; set; }

            IMyRemoteControl RemoteControl { get; set; }

            public double VelocityMagnitude { get; set; }
            public Vector3D VelocityComponents { get; set; }

            public Vector3D CurrentPosition { get; set; }
            public Vector3D StartPosition { get; set; }
            public Vector3D PreviousPosition { get; set; }
            public NTable VelocityNTable { get; set; }

            public MatrixD CurrentOrientation { get; set; }
            public MatrixD PreviousOrientation { get; set; }

            public IMyThrust[] AllThrusters { get; set; }
            public IMyGyro[] AllGyroscopes { get; set; }

            STUVelocityController VelocityController { get; set; }
            STUOrientationController OrientationController { get; set; }

            STUMasterLogBroadcaster Broadcaster { get; set; }

            /// <summary>
            /// Flight utility class that handles velocity control and orientation control. Requires exactly one Remote Control block to function.
            /// Be sure to orient the Remote Control block so that its forward direction is the direction you want to be considered the "forward" direction of your ship.
            /// Also orient the Remote Control block so that its up direction is the direction you want to be considered the "up" direction of your ship.
            /// You can also pass in an optional NTable if you'd like to adjust how the ship's velocity is controlled. Higher values will result in more aggressive deceleration.
            /// </summary>
            public STUFlightController(IMyRemoteControl remoteControl, float timeStep, IMyThrust[] allThrusters, IMyGyro[] allGyros, STUMasterLogBroadcaster broadcaster, NTable Ntable = null) {
                TimeStep = timeStep;
                RemoteControl = remoteControl;
                AllGyroscopes = allGyros;
                AllThrusters = allThrusters;
                VelocityNTable = Ntable;

                VelocityController = new STUVelocityController(RemoteControl, TimeStep, AllThrusters, broadcaster, VelocityNTable);
                OrientationController = new STUOrientationController(RemoteControl, AllGyroscopes);
                // Force dampeners on for the time being; will get turned off on launch
                RemoteControl.DampenersOverride = true;

                Update();

                Broadcaster = broadcaster;
            }

            public void MeasureCurrentVelocity() {
                Vector3D worldVelocity = RemoteControl.GetShipVelocities().LinearVelocity;
                Vector3D localVelocity = Vector3D.TransformNormal(worldVelocity, MatrixD.Transpose(PreviousOrientation));
                // Space Engineers considers the missile's forward direction (the direction it's facing) to be in the negative Z direction
                // We reverse that by convention because it's easier to think about
                VelocityComponents = localVelocity *= new Vector3D(1, 1, -1);
                VelocityMagnitude = VelocityComponents.Length();
            }

            public void MeasureCurrentPositionAndOrientation() {
                CurrentOrientation = RemoteControl.WorldMatrix;
                CurrentPosition = RemoteControl.GetPosition();
            }

            public void Update() {
                MeasureCurrentPositionAndOrientation();
                MeasureCurrentVelocity();
                PreviousOrientation = CurrentOrientation;
                PreviousPosition = CurrentPosition;
            }

            /// <summary>
            /// Sets the ship's velocity in the forward direction. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetVx(double desiredVelocity) {
                return VelocityController.ControlVx(VelocityComponents.X, desiredVelocity);
            }

            /// <summary>
            /// Sets the ship's rightward velocity. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetVy(double desiredVelocity) {
                return VelocityController.ControlVy(VelocityComponents.Y, desiredVelocity);
            }

            /// <summary>
            /// Sets the ship's upward velocity. Returns true if the ship's velocity is stable.
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetVz(double desiredVelocity) {
                return VelocityController.ControlVz(VelocityComponents.Z, desiredVelocity);
            }

            /// <summary>
            /// Sets the ship into a steady forward flight while controlling lateral thrusters. Good for turning while maintaining a forward velocity.
            /// </summary>
            /// <param name="desiredVelocity"></param>
            /// <returns></returns>
            public bool SetStableForwardVelocity(double desiredVelocity) {
                bool forwardStable = SetVz(desiredVelocity);
                bool rightStable = SetVx(0);
                bool upStable = SetVy(0);
                return forwardStable && rightStable && upStable;
            }

            public bool StableFreeFall() {
                bool rightStable = SetVx(0);
                bool upStable = SetVy(0);
                return rightStable && upStable;
            }

            public bool OrientShip(Vector3D targetPos) {
                return OrientationController.AlignShipToTarget(targetPos, CurrentPosition);
            }

            private Vector3D GetInertiaHeadingNormal(Vector3D targetPos, Vector3D MOCK_INERTIA_VECTOR) {
                // ship inertia vector
                Vector3D SI = MOCK_INERTIA_VECTOR;
                SI = Vector3D.Normalize(Vector3D.TransformNormal(SI, RemoteControl.WorldMatrix));
                // ship-to-target vector
                Vector3D ST = Vector3D.Normalize(targetPos - CurrentPosition);
                // normal vector of plane containing SI and ST
                Vector3D crossProduct = Vector3D.Cross(SI, ST);

                if (Math.Abs(crossProduct.Length()) < 0.01) {
                    Broadcaster.Log(new STULog {
                        Sender = LIGMA.MissileName,
                        Message = "Cross product SI X ST = 0",
                        Type = STULogType.WARNING,
                        Metadata = LIGMA.GetTelemetryDictionary()
                    });
                    return Vector3D.Zero;
                }

                return Vector3D.Normalize(crossProduct);
            }

            private double CalculateRollAngle(Vector3D normalVector, Vector3D lateralFaceNormal) {
                var dotProduct = Vector3D.Dot(normalVector, lateralFaceNormal);
                var angle = Math.Acos(dotProduct);
                return angle - Math.PI / 4;
            }

            public void AdjustShipRoll(Vector3D targetPos, Vector3D MOCK_INERTIA_VECTOR) {
                Vector3D inertiaHeadingNormal = GetInertiaHeadingNormal(targetPos, VelocityComponents);
                if (inertiaHeadingNormal == Vector3D.Zero) { return; }

                // Get the current orientation of the ship
                MatrixD currentOrientation = RemoteControl.WorldMatrix.GetOrientation();

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
                    Broadcaster.Log(new STULog {
                        Sender = LIGMA.MissileName,
                        Message = $"Roll complete",
                        Type = STULogType.OK,
                        Metadata = LIGMA.GetTelemetryDictionary(),
                    });
                    OrientationController.SetRoll(0);

                    // Otherwise, apply roll
                } else {
                    Broadcaster.Log(new STULog {
                        Sender = "Roll Adjustment",
                        Message = $"Roll adjustment: {rollAdjustment} radians",
                        Type = STULogType.WARNING,
                        Metadata = LIGMA.GetTelemetryDictionary(),
                    });
                    OrientationController.SetRoll(rollAdjustment);
                }
            }

            public void SetRoll(double roll) {
                OrientationController.SetRoll(roll);
            }

        }
    }
}
