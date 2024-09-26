using Sandbox.Game.WorldEnvironment.Modules;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            public class HardStop : ManeuverTemplate
            {
                public override string Name => "Hard Stop";

                private double oneTickAcceleration;
                private STUFlightController FC;

                public HardStop(STUFlightController thisFlightController)
                {
                    oneTickAcceleration = 0;
                    FC = thisFlightController;
                }

                public override bool Init()
                {
                    CreateWarningFlightLog("Initiating hard stop! User controls disabled");

                    // Determine the maximum acceleration the ship can exert per tick
                    double maxAcceleration = FC.VelocityController.MaximumThrustVector.Length() / FC.GetShipMass();
                    oneTickAcceleration = Math.Ceiling(maxAcceleration / 6.0);
                    FC.ReinstateGyroControl();
                    FC.ReinstateThrusterControl();

                    // Make sure all thrusters are on
                    foreach (var thruster in FC.ActiveThrusters)
                    {
                        thruster.Enabled = true;
                    }

                    FC.RemoteControl.DampenersOverride = false;

                    return true;
                }

                public override bool Run()
                {
                    Vector3D worldLinearVelocity = FC.RemoteControl.GetShipVelocities().LinearVelocity;
                    FC.VelocityController.ExertVectorForce_WorldFrame(-worldLinearVelocity, float.PositiveInfinity);
                    FC.OrientationController.AlignCounterVelocity(worldLinearVelocity, FC.VelocityController.MaximumThrustVector);

                    if (worldLinearVelocity.Length() < oneTickAcceleration)
                    {
                        CreateOkFlightLog("Hard stop complete! Returning controls to user");
                        return true;
                    }

                    return false;
                }

                public override bool Closeout()
                {
                    FC.RemoteControl.DampenersOverride = true;
                    FC.RelinquishGyroControl();
                    FC.RelinquishThrusterControl();

                    return true;
                }
                public override void Update() 
                {
                    switch (CurrentInternalState)
                    {
                        case InternalStates.Init:
                            if (Init())
                            {
                                CurrentInternalState = InternalStates.Run;
                            }
                            break;
                        case InternalStates.Run:
                            if (Run())
                            {
                                CurrentInternalState = InternalStates.Closeout;
                            }
                            break;
                        case InternalStates.Closeout:
                            if (Closeout())
                            {
                                CurrentInternalState = InternalStates.Done;
                            }
                            break;
                        case InternalStates.Done:
                            break;
                    }
                }
            }
        }
    }
}