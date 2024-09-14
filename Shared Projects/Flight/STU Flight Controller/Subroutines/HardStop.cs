using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            enum HardStopState {
                Initialize,
                Aligning,
                FireThrusters,
                Stopping,
                Terminate
            }

            HardStopState CurrentState = HardStopState.Initialize;

            public bool HardStop() {

                switch (CurrentState) {

                    case HardStopState.Initialize:
                        ReinstateGyroControl();
                        ReinstateThrusterControl();
                        RemoteControl.DampenersOverride = false;
                        CreateWarningFlightLog("Initiating hard stop!");
                        CurrentState = HardStopState.Aligning;
                        break;

                    case HardStopState.Aligning:
                        Vector3D worldLinearVelocity = RemoteControl.GetShipVelocities().LinearVelocity;
                        bool alignedAgainstCurrentVelocity = OrientationController.AlignCounterVelocity(
                            worldLinearVelocity,
                            VelocityController.MaximumThrustVector
                        );

                        // While rotating, fire thrusters in the opposite direction of the current velocity
                        Vector3D counterVelocity = -worldLinearVelocity;
                        counterVelocity.Normalize();
                        // We are limited by the minimum thrust vector
                        counterVelocity *= VelocityController.MinimumThrustVector.Length();
                        Vector3D tempCounterVelocityVector = Vector3D.TransformNormal(counterVelocity, MatrixD.Transpose(CurrentWorldMatrix)) * new Vector3D(1, 1, -1);
                        ExertVectorForce(tempCounterVelocityVector);

                        if (alignedAgainstCurrentVelocity) {
                            CreateInfoFlightLog("Aligned against current velocity; initiating slowdown burn");
                            CurrentState = HardStopState.FireThrusters;
                        }
                        break;

                    case HardStopState.FireThrusters:
                        CreateInfoFlightLog($"Thruster count: {VelocityController.MaximumThrustVectorThrusters.Length}");
                        foreach (var thruster in VelocityController.MaximumThrustVectorThrusters) {
                            thruster.Enabled = true;
                            thruster.ThrustOverride = thruster.MaxEffectiveThrust;
                        }
                        CreateInfoFlightLog("Monitoring velocity for terminate condition");
                        CurrentState = HardStopState.Stopping;
                        break;

                    case HardStopState.Stopping:
                        double maxAcceleration = VelocityController.MaximumThrustVector.Length() / GetShipMass();
                        double oneTickAcceleration = Math.Ceiling(maxAcceleration / 6.0);
                        if (CurrentVelocity.Length() < oneTickAcceleration) {
                            CreateOkFlightLog($"Velocity below {oneTickAcceleration} m/s; terminating hard stop");
                            CurrentState = HardStopState.Terminate;
                        }
                        break;

                    case HardStopState.Terminate:
                        RemoteControl.DampenersOverride = true;
                        RelinquishGyroControl();
                        RelinquishThrusterControl();
                        return true;

                }

                return false;

            }

        }
    }
}