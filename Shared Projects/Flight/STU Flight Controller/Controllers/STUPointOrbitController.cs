using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public partial class STUPointOrbitController {

                enum PointOrbitState {
                    Idle,
                    EnteringOrbit,
                    Orbiting,
                    StopOrbit
                }

                PointOrbitState CurrentState = PointOrbitState.Idle;
                STUFlightController FlightController { get; set; }
                IMyRemoteControl RemoteControl { get; set; }

                private double TargetRadius { get; set; }

                public STUPointOrbitController(STUFlightController flightController, IMyRemoteControl remoteControl) {
                    FlightController = flightController;
                    RemoteControl = remoteControl;
                }

                public bool Run(Vector3D targetPos) {
                    switch (CurrentState) {
                        case PointOrbitState.Idle:
                            // do nothing
                            CurrentState = PointOrbitState.EnteringOrbit;
                            break;
                        case PointOrbitState.EnteringOrbit:
                            if (EnterOrbit(targetPos)) {
                                CurrentState = PointOrbitState.Orbiting;
                                // Lock-in the current radius for PID purposes
                                TargetRadius = Vector3D.Distance(targetPos, RemoteControl.CenterOfMass);
                            }
                            break;
                        case PointOrbitState.Orbiting:
                            ExertCentripitalForce(targetPos);
                            break;
                    }
                    return false;
                }

                private bool EnterOrbit(Vector3D targetPos) {
                    double velocity = FlightController.CurrentVelocity.Length();
                    if (velocity < 10) {
                        Vector3D radiusVector = targetPos - FlightController.CurrentPosition;
                        Vector3D nonColinearVector = new Vector3D(0, 0, 1);
                        Vector3D initialOrbitVector = Vector3D.Cross(radiusVector, nonColinearVector);
                        Vector3D kickstartThrust = Vector3D.Normalize(initialOrbitVector) * STUVelocityController.ShipMass;
                        LIGMA.CreateOkBroadcast($"kickstart: {kickstartThrust}");
                        FlightController.ExertVectorForce(kickstartThrust);
                        return false;
                    }
                    return true;
                }


                public void ExertCentripitalForce(Vector3D targetPos) {
                    double mass = STUVelocityController.ShipMass;
                    double velocity = FlightController.CurrentVelocity.Length();
                    double velocitySquared = velocity * velocity;
                    double radius = Vector3D.Distance(targetPos, RemoteControl.CenterOfMass);
                    double radiusError = radius - TargetRadius;
                    // create a vector that points from the ship to the target
                    // if velocity is really close to zero, we need to kickstart an orbit
                    double centripetalForce = ((mass * velocitySquared) / radius) + 100 * radiusError;
                    Vector3D centriptalForceVector = Vector3D.Normalize(targetPos - RemoteControl.CenterOfMass) * centripetalForce;
                    LIGMA.CreateOkBroadcast($"F_c_adj = {centripetalForce}");
                    LIGMA.CreateOkBroadcast($"F_c_raw = {(mass * velocitySquared) / radius}");
                    LIGMA.CreateOkBroadcast($"V_c = {FlightController.CurrentVelocity.Length()}");
                    LIGMA.CreateOkBroadcast($"r = {radius}");
                    LIGMA.CreateOkBroadcast($"r_e = {radiusError}");
                    LIGMA.CreateOkBroadcast($"r_t = {TargetRadius}");
                    FlightController.ExertVectorForce(Vector3D.TransformNormal(centriptalForceVector, MatrixD.Transpose(FlightController.CurrentWorldMatrix)) * new Vector3D(1, 1, -1));
                }
            }
        }
    }
}
