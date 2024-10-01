﻿using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            public class HardStop : STUStateMachine {
                public override string Name => "Hard Stop";

                private STUFlightController FC;

                private double oneTickAcceleration;

                public HardStop(STUFlightController thisFlightController) {
                    oneTickAcceleration = 0;
                    FC = thisFlightController;
                }

                protected override bool Init() {
                    CreateWarningFlightLog("Initiating hard stop! User controls disabled");
                    // Determine the maximum acceleration the ship can exert per tick
                    double maxAcceleration = FC.VelocityController.MaximumThrustVector.Length() / FC.GetShipMass();
                    oneTickAcceleration = Math.Ceiling(maxAcceleration / 6.0);
                    FC.ReinstateGyroControl();
                    FC.ReinstateThrusterControl();
                    // Make sure all thrusters are on
                    foreach (var thruster in FC.ActiveThrusters) {
                        thruster.Enabled = true;
                    }
                    FC.RemoteControl.DampenersOverride = false;
                    return true;
                }

                protected override bool Run() {
                    Vector3D worldLinearVelocity = FC.RemoteControl.GetShipVelocities().LinearVelocity;
                    FC.VelocityController.ExertVectorForce_WorldFrame(-worldLinearVelocity, float.PositiveInfinity);
                    FC.OrientationController.AlignCounterVelocity(worldLinearVelocity, FC.VelocityController.MaximumThrustVector);
                    if (worldLinearVelocity.Length() < oneTickAcceleration) {
                        CreateOkFlightLog("Hard stop complete! Returning controls to user");
                        return true;
                    }
                    return false;
                }

                protected override bool Closeout() {
                    FC.RemoteControl.DampenersOverride = true;
                    FC.RelinquishGyroControl();
                    FC.RelinquishThrusterControl();
                    return true;
                }

            }
        }
    }
}