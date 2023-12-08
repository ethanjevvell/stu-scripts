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
            public STUFlightController(IMyRemoteControl remoteControl, float timeStep, IMyThrust[] allThrusters, IMyGyro[] allGyros, NTable Ntable = null) {

                TimeStep = timeStep;
                RemoteControl = remoteControl;
                AllGyroscopes = allGyros;
                AllThrusters = allThrusters;
                VelocityNTable = Ntable;

                VelocityController = new STUVelocityController(RemoteControl, TimeStep, AllThrusters, RemoteControl.CalculateShipMass().TotalMass, VelocityNTable);
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

            public bool ControlForward(double desiredVelocity) {
                return VelocityController.ControlForward(VelocityComponents.Z, desiredVelocity);
            }

            public bool ControlUp(double desiredVelocity) {
                return VelocityController.ControlUp(VelocityComponents.Y, desiredVelocity);
            }

            public bool ControlRight(double desiredVelocity) {
                return VelocityController.ControlRight(VelocityComponents.X, desiredVelocity);
            }

            public bool OrientShip(Vector3D target) {
                return OrientationController.AlignShip(target, CurrentPosition);
            }

        }
    }
}
