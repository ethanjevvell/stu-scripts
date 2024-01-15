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

            public float CurrentStoppingDistance { get; set; }

            public Vector3D CurrentPosition { get; set; }
            public Vector3D PreviousPosition { get; set; }
            public NTable VelocityNTable { get; set; }

            public MatrixD CurrentWorldMatrix { get; set; }
            public MatrixD PreviousWorldMatrix { get; set; }

            public IMyThrust[] AllThrusters { get; set; }
            public IMyGyro[] AllGyroscopes { get; set; }

            STUVelocityController VelocityController { get; set; }
            STUOrientationController OrientationController { get; set; }

            /// <summary>
            /// Flight utility class that handles velocity control and orientation control. Requires exactly one Remote Control block to function.
            /// Be sure to orient the Remote Control block so that its forward direction is the direction you want to be considered the "forward" direction of your ship.
            /// Also orient the Remote Control block so that its up direction is the direction you want to be considered the "up" direction of your ship.
            /// You can also pass in an optional NTable if you'd like to adjust how the ship's velocity is controlled. Higher values will result in more aggressive deceleration.
            /// </summary>
            public STUFlightController(IMyRemoteControl remoteControl, float timeStep, IMyThrust[] allThrusters, IMyGyro[] allGyros, NTable Ntable = null) {
                TimeStep = timeStep;
                RemoteControl = remoteControl;
                AllGyroscopes = allGyros;
                AllThrusters = allThrusters;
                VelocityNTable = Ntable;

                VelocityController = new STUVelocityController(RemoteControl, TimeStep, AllThrusters, VelocityNTable);
                OrientationController = new STUOrientationController(RemoteControl, AllGyroscopes);

                // Force dampeners on for the time being; will get turned off on launch
                RemoteControl.DampenersOverride = true;

                Update();
            }

            public void MeasureCurrentVelocity() {
                Vector3D worldVelocity = RemoteControl.GetShipVelocities().LinearVelocity;
                Vector3D localVelocity = Vector3D.TransformNormal(worldVelocity, MatrixD.Transpose(PreviousWorldMatrix));
                // Space Engineers considers the missile's forward direction (the direction it's facing) to be in the negative Z direction
                // We reverse that by convention because it's easier to think about
                VelocityComponents = localVelocity *= new Vector3D(1, 1, -1);
                VelocityMagnitude = VelocityComponents.Length();
            }

            public void MeasureCurrentPositionAndOrientation() {
                CurrentWorldMatrix = RemoteControl.WorldMatrix;
                CurrentPosition = RemoteControl.GetPosition();
            }

            public float GetForwardStoppingDistance() {
                float mass = STUVelocityController.ShipMass;
                float velocity = (float)VelocityMagnitude;
                float force = (float)VelocityController.MaximumReverseAcceleration;
                float dx = ((-velocity * velocity) * mass) / (2 * force); //kinematic equation for "how far would I travel if I slammed on the brakes right now?"
                return dx;
            }

            public void Update() {
                MeasureCurrentPositionAndOrientation();
                MeasureCurrentVelocity();
                PreviousWorldMatrix = CurrentWorldMatrix;
                PreviousPosition = CurrentPosition;
                CurrentStoppingDistance = GetForwardStoppingDistance();
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
            /// Sets the ship's roll. Positive values roll the ship clockwise, negative values roll the ship counterclockwise.
            /// </summary>
            /// <param name="roll"></param>
            public void SetVr(double roll) {
                OrientationController.SetRoll(roll);
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
            public bool AlignShipToTarget(Vector3D targetPos) {
                return OrientationController.AlignShipToTarget(targetPos, CurrentPosition);
            }

            /// <summary>
            /// Rolls the ship to optimize the ship's inertia vector for a given target position. Effectively allows the ship to turn faster, at the cost of more fuel.
            /// </summary>
            /// <param name="targetPos"></param>
            /// <param name="MOCK_INERTIA_VECTOR"></param>
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
                    OrientationController.SetRoll(0);
                } else {
                    OrientationController.SetRoll(rollAdjustment);
                }
            }

            /// <summary>
            /// Calculates a normal vector for the plane containing the ship's inertia vector and the vector from the ship to the target.
            /// </summary>
            /// <param name="targetPos"></param>
            /// <param name="MOCK_INERTIA_VECTOR"></param>
            /// <returns></returns>
            private Vector3D GetInertiaHeadingNormal(Vector3D targetPos) {
                // ship inertia vector
                Vector3D worldVelocity = Vector3D.Normalize(Vector3D.TransformNormal(VelocityComponents, CurrentWorldMatrix));
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

            public void UpdateShipMass() {
                STUVelocityController.ShipMass = RemoteControl.CalculateShipMass().PhysicalMass;
            }
        }
    }
}
