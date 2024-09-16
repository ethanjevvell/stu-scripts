using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            enum HardStopState {
                Initialize,
                Stopping,
                Terminate
            }

            HardStopState CurrentState = HardStopState.Initialize;
            double oneTickAcceleration = 0;

            public bool HardStop() {

                switch (CurrentState) {

                    case HardStopState.Initialize:
                        CreateWarningFlightLog("Initiating hard stop! User controls disabled");
                        // Determine the maximum acceleration the ship can exert per tick
                        double maxAcceleration = VelocityController.MaximumThrustVector.Length() / GetShipMass();
                        oneTickAcceleration = Math.Ceiling(maxAcceleration / 6.0);
                        ReinstateGyroControl();
                        ReinstateThrusterControl();

                        // Make sure all thrusters are on
                        foreach (var thruster in ActiveThrusters) {
                            thruster.Enabled = true;
                        }

                        RemoteControl.DampenersOverride = false;
                        CurrentState = HardStopState.Stopping;
                        break;

                    case HardStopState.Stopping:
                        Vector3D worldLinearVelocity = RemoteControl.GetShipVelocities().LinearVelocity;
                        VelocityController.ExertVectorForce_WorldFrame(-worldLinearVelocity, float.PositiveInfinity);
                        OrientationController.AlignCounterVelocity(
                            worldLinearVelocity,
                            VelocityController.MaximumThrustVector
                        );
                        if (worldLinearVelocity.Length() < oneTickAcceleration) {
                            CreateOkFlightLog("Hard stop complete! Returning controls to user");
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