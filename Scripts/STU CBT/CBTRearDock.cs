using Sandbox.Game.Screens.DebugScreens;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBTRearDock
        {
            // variables
            private const float HINGE_ANGLE_TOLERANCE = 0.0071f;
            private const float HINGE_TARGET_VELOCITY = 1f;
            private const float HINGE_TORQUE = 7000000;
            private const float PISTON_POSITION_TOLERANCE = 0.01f;
            private const float PISTON_TARGET_VELOCITY = 1.4f;
            public IMyPistonBase RearDockPiston { get; set; }
            public IMyMotorStator RearDockHinge1 { get; set; }
            public IMyMotorStator RearDockHinge2 { get; set; }
            public IMyShipConnector RearDockConnector { get; set; }

            private float PistonTargetDistance { get; set; }
            private float Hinge1TargetAngle { get; set; }
            private float Hinge2TargetAngle { get; set; }

            public enum RearDockStates
            {
                Unknown,
                Retracted,
                Retracting,
                RetractingHinge1,
                RetractingHinge2,
                RetractingPiston,
                Extended,
                Extending,
                ExtendingHinge1,
                ExtendingHinge2,
                ExtendingPiston,
                Resetting,
                ResettingPiston,
                ResettingHinge1,
                ResettingHinge2,
                ResettingToRetracted,
                Frozen,
            }

            public Dictionary<string, float[]> KnownPorts = new Dictionary<string, float[]>()
            {
                // { "PortName", new float[] { pistonDistance (m), Hinge1Angle (radians), Hinge2Angle (radians) } }
                { "Lunar HQ", new float[] { 10, (float)(36 / (180/Math.PI)), (float)(-36 / (180 / Math.PI)) } },
                { "Generic Ship", new float[] { 5, 0, 0 } },
            };

            public RearDockStates CurrentRearDockState { get; set; } 
            public RearDockStates LastUserInputRearDockState { get; set; }

            public Dictionary<RearDockStates, List<RearDockStates>> ValidStateTransitions = new Dictionary<RearDockStates, List<RearDockStates>>()
            {
                { RearDockStates.Unknown, 
                    new List<RearDockStates> {} }, // any state can go to Unknown

                { RearDockStates.Retracted, 
                    new List<RearDockStates> { 
                        RearDockStates.Unknown,
                        RearDockStates.RetractingHinge1, } }, // can only get to Retracted from RetractingHinge1, or can technically get there from Unknown if TryDetermineState() is true

                { RearDockStates.RetractingHinge1, 
                    new List<RearDockStates> { 
                        RearDockStates.RetractingHinge2 } }, // can only get to RetractingHinge1 from RetractingHinge2

                { RearDockStates.RetractingHinge2,
                    new List<RearDockStates> {
                        RearDockStates.Extended,
                        RearDockStates.Frozen } }, // can initiate retraction from Extended or Frozen

                { RearDockStates.RetractingPiston,
                    new List<RearDockStates> {
                        RearDockStates.RetractingHinge1 } }, // can only retract the piston after both hinges (specifically Hinge1, which follows after Hinge2) are retracted

                { RearDockStates.Extended, 
                    new List<RearDockStates> { 
                        RearDockStates.ExtendingHinge2 } }, // can only get to Extended from ExtendingHinge2

                { RearDockStates.ExtendingHinge1, 
                    new List<RearDockStates> { 
                        RearDockStates.Retracted, } }, // can only get to ExtendingHinge1 from Retracted

                { RearDockStates.ExtendingHinge2,
                    new List<RearDockStates> {
                        RearDockStates.ExtendingHinge1, } }, // can only get to ExtendingHinge2 from ExtendingHinge1

                { RearDockStates.ExtendingPiston,
                    new List<RearDockStates> {
                        RearDockStates.Retracted,
                        RearDockStates.Frozen, } }, // can initiate Extension from Retracted or Frozen

                { RearDockStates.Resetting,
                    new List<RearDockStates> {} }, // any state can go to Resetting

                { RearDockStates.ResettingPiston,
                    new List<RearDockStates>
                    {
                        RearDockStates.ResettingHinge1 } }, // can only reset the piston after both hinges (specifically Hinge1, which follows after Hinge2) are reset

                { RearDockStates.ResettingHinge1,
                    new List<RearDockStates>
                    {
                        RearDockStates.ResettingHinge2 } }, // can only reset Hinge1 after Hinge2 is reset

                { RearDockStates.ResettingHinge2,
                    new List<RearDockStates>
                    {
                        RearDockStates.Retracted } }, // can only reset Hinge2 after Hinge1 is reset

                { RearDockStates.Frozen,
                    new List<RearDockStates> {} }, // any state can go to Frozen
            };

            //constructor
            public CBTRearDock(IMyPistonBase piston, IMyMotorStator hinge1, IMyMotorStator hinge2, IMyShipConnector connector)
            {
                RearDockPiston = piston;
                RearDockHinge1 = hinge1;
                RearDockHinge2 = hinge2;
                RearDockConnector = connector;

                CurrentRearDockState = RearDockStates.Unknown;
                LastUserInputRearDockState = CBT.UserInputRearDockState;

                RearDockHinge1.BrakingTorque = HINGE_TORQUE;
                RearDockHinge2.BrakingTorque = HINGE_TORQUE;

                switch (TryDetermineState())
                {
                    case RearDockStates.Retracted:
                        CurrentRearDockState = RearDockStates.Retracted;
                        break;
                    default:
                        CurrentRearDockState = RearDockStates.Unknown;
                        break;
                }
            }

            // state machine
            public void UpdateRearDock(RearDockStates desiredState)
            {
                if (desiredState != CurrentRearDockState && desiredState != LastUserInputRearDockState)
                {
                    if (CanGoToRequestedState(desiredState))
                    {
                        CurrentRearDockState = desiredState;
                        LastUserInputRearDockState = desiredState;
                    }
                    else
                    {
                        CBT.AddToLogQueue("Invalid Rear Dock state transition requested.", STULogType.ERROR);
                    }
                }
                    
                switch (CurrentRearDockState)
                {
                    case RearDockStates.Unknown:
                        // do nothing
                        break;

                    case RearDockStates.Retracted:
                        // do nothing
                        break;

                    case RearDockStates.RetractingHinge1:
                        RetractRearDock();
                        break;

                    case RearDockStates.RetractingHinge2:
                        RetractRearDock();
                        break;

                    case RearDockStates.RetractingPiston:
                        RetractRearDock();
                        break;

                    case RearDockStates.Extended:
                        // do nothing
                        break;

                    case RearDockStates.Extending:
                        float[] portParameters;
                        if (KnownPorts.TryGetValue(CBT.UserInputRearDockPort, out portParameters)) 
                        { 
                            SetupExtension(portParameters);
                            CurrentRearDockState = RearDockStates.ExtendingPiston;
                        } 
                        else { CBT.AddToLogQueue("Invalid Rear Dock port configuration requested.", STULogType.ERROR); }
                        break;

                    case RearDockStates.ExtendingHinge1:
                        ExtendRearDock();
                        break;

                    case RearDockStates.ExtendingHinge2:
                        ExtendRearDock();
                        break;

                    case RearDockStates.ExtendingPiston:
                        ExtendRearDockPiston();
                        break;

                    case RearDockStates.Resetting:
                        ResetRearDock();
                        CurrentRearDockState = RearDockStates.ResettingPiston;
                        break;

                    case RearDockStates.ResettingPiston:
                        if (ResetPiston()) CurrentRearDockState = RearDockStates.ResettingHinge1;
                        break;

                    case RearDockStates.ResettingHinge1:
                        if (ResetHinge1()) CurrentRearDockState = RearDockStates.ResettingHinge2;
                        break;

                    case RearDockStates.ResettingHinge2:
                        if (ResetHinge2()) CurrentRearDockState = RearDockStates.ResettingToRetracted;
                        break;

                    case RearDockStates.ResettingToRetracted:
                        if (ResetToRetracted()) CurrentRearDockState = RearDockStates.Retracted;
                        break;

                    case RearDockStates.Frozen: // currently unused
                        CBT.AddToLogQueue("Halting Rear Dock actuators.", STULogType.INFO);
                        RearDockHinge1.TargetVelocityRPM = 0;
                        RearDockHinge2.TargetVelocityRPM = 0;
                        RearDockPiston.Velocity = 0;
                        break;

                    default:
                        break;
                }
            }

            // methods
            private RearDockStates TryDetermineState()
            {
                if (
                    RearDockPiston.CurrentPosition - RearDockPiston.MinLimit < PISTON_POSITION_TOLERANCE
                    && RearDockHinge1.Angle - Math.PI / 2 < HINGE_ANGLE_TOLERANCE
                    && RearDockHinge2.Angle - Math.PI / 2 < HINGE_ANGLE_TOLERANCE
                    )
                {
                    return RearDockStates.Retracted;
                }
                else { return RearDockStates.Unknown; }
            }

            public bool CanGoToRequestedState(RearDockStates requestedState)
            {
                if (ValidStateTransitions[CurrentRearDockState].Contains(requestedState)) { return true; }
                else return false;
            }

            private void ResetRearDock()
            {
                RearDockConnector.Disconnect();

                RearDockHinge1.Torque = 0;
                RearDockHinge1.TargetVelocityRPM = 0;
                RearDockHinge1.RotorLock = false;
                RearDockHinge1.UpperLimitDeg = 90;
                RearDockHinge1.LowerLimitDeg = -90;

                RearDockHinge2.Torque = 0;
                RearDockHinge2.TargetVelocityRPM = 0;
                RearDockHinge2.RotorLock = false;
                RearDockHinge2.UpperLimitDeg = 90;
                RearDockHinge2.LowerLimitDeg = -90;

                RearDockPiston.MinLimit = 0;
                RearDockPiston.MaxLimit = 10;
                RearDockPiston.Velocity = 0;
            }

            private bool ResetPiston()
            {
                RearDockPiston.Velocity = PISTON_TARGET_VELOCITY;
                return RearDockPiston.CurrentPosition - RearDockPiston.MaxLimit < PISTON_POSITION_TOLERANCE;
            }

            private bool ResetHinge1()
            {
                RearDockHinge1.Torque = HINGE_TORQUE;
                if (Math.Abs(RearDockHinge1.Angle) < HINGE_ANGLE_TOLERANCE)
                {
                    RearDockHinge1.TargetVelocityRPM = 0;
                    return true;
                }
                else if (HINGE_ANGLE_TOLERANCE - RearDockHinge1.Angle < 0)
                {
                    RearDockHinge1.TargetVelocityRPM = HINGE_TARGET_VELOCITY;
                    return false;
                }
                else if (HINGE_ANGLE_TOLERANCE - RearDockHinge1.Angle > 0)
                {
                    RearDockHinge1.TargetVelocityRPM = -HINGE_TARGET_VELOCITY;
                    return false;
                }
                else return false;
            }

            private bool ResetHinge2()
            {
                RearDockHinge2.Torque = HINGE_TORQUE;
                RearDockHinge2.TargetVelocityRPM = HINGE_TARGET_VELOCITY;
                if (Math.Abs(RearDockHinge2.Angle - Math.PI / 2) < HINGE_ANGLE_TOLERANCE)
                {
                    RearDockHinge2.TargetVelocityRPM = 0;
                    return true;
                }
                else return false;
            }

            private bool ResetToRetracted()
            {
                RearDockHinge2.LowerLimitDeg = -90;
                RearDockHinge2.TargetVelocityRPM = -HINGE_TARGET_VELOCITY;

                RearDockPiston.MinLimit = 0;
                RearDockPiston.Velocity = -PISTON_TARGET_VELOCITY;
                return Math.Abs(RearDockHinge2.Angle - Math.PI / 2) < HINGE_ANGLE_TOLERANCE && RearDockPiston.CurrentPosition < PISTON_POSITION_TOLERANCE;
            }

            private void SetupExtension(float[] portParameters)
            {
                PistonTargetDistance = portParameters[0];
                Hinge1TargetAngle = portParameters[1];
                Hinge2TargetAngle = portParameters[2];
            }

            private bool ExtendPiston()
        }
    }
}
