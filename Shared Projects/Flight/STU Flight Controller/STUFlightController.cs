using Sandbox.ModAPI.Ingame;
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

                Update();
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

            public bool OrientShip(Vector3D target) {
                return OrientationController.AlignShipToTarget(target, CurrentPosition);
            }

        }
    }
}
