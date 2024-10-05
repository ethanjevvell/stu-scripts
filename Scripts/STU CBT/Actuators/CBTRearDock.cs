using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

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
                Moved,
                Moving,
                MovingHinge1,
                MovingHinge2,
                MovingPiston,
                Resetting,
                ResettingPiston,
                ResettingHinge1,
                ResettingHinge2,
                Frozen,
            }

            public struct ActuatorPosition
            {
                public float PistonDistance;
                public float Hinge1Angle;
                public float Hinge2Angle;
            }

            public Dictionary<string, ActuatorPosition> KnownPorts = new Dictionary<string, ActuatorPosition>()
            {
                { "STOWED", new ActuatorPosition { PistonDistance = 0, Hinge1Angle = (float)(Math.PI / 2), Hinge2Angle = (float)(Math.PI / 2) } },
                { "LUNAR HQ", new ActuatorPosition { PistonDistance = 10, Hinge1Angle = (float)(36 / (180 / Math.PI)), Hinge2Angle = (float)(-36 / (180 / Math.PI)) } },
                { "SHIP ON DECK", new ActuatorPosition { PistonDistance = 0, Hinge1Angle = (float)(-Math.PI / 2), Hinge2Angle = (float)(-Math.PI / 2) } },
                { "NEUTRAL", new ActuatorPosition { PistonDistance = 5, Hinge1Angle = 0, Hinge2Angle = 0 } },
            };

            public RearDockStates CurrentRearDockState { get; set; } 
            public RearDockStates LastUserInputRearDockState { get; set; }
            public RearDockStates DestinationAfterNeutral { get; set; }

            public Dictionary<RearDockStates, List<RearDockStates>> ValidStateTransitions = new Dictionary<RearDockStates, List<RearDockStates>>()
            {
                { RearDockStates.Unknown,
                    new List<RearDockStates> {} }, // any state can go to Unknown

                { RearDockStates.Moved,
                    new List<RearDockStates> {
                        RearDockStates.MovingHinge2 } }, // can only get to Extended from ExtendingHinge2

                { RearDockStates.MovingHinge1,
                    new List<RearDockStates> { } }, // NEED TO FIX THIS

                { RearDockStates.MovingHinge2,
                    new List<RearDockStates> {
                        RearDockStates.MovingHinge1, } }, // can only get to ExtendingHinge2 from ExtendingHinge1

                { RearDockStates.MovingPiston,
                    new List<RearDockStates> { } }, // NEED TO FIX THIS

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
                    new List<RearDockStates> { } }, // NEED TO FIX THIS

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

                CurrentRearDockState = TryDetermineState(); 
                LastUserInputRearDockState = CBT.UserInputRearDockState;

                RearDockHinge1.BrakingTorque = HINGE_TORQUE;
                RearDockHinge2.BrakingTorque = HINGE_TORQUE;
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

                    case RearDockStates.Moving:
                        ActuatorPosition portParameters;
                        if (KnownPorts.TryGetValue(CBT.UserInputRearDockPort, out portParameters)) 
                        { 
                            SetUpExtension(portParameters);
                            CurrentRearDockState = RearDockStates.MovingPiston;
                        } 
                        else { CBT.AddToLogQueue("Invalid Rear Dock port configuration requested.", STULogType.ERROR); }
                        break;

                    case RearDockStates.MovingPiston:
                        if (MovePiston()) { CurrentRearDockState = RearDockStates.MovingHinge1; }
                        break;

                    case RearDockStates.MovingHinge1:
                        if (MoveHinge1()) { CurrentRearDockState = RearDockStates.MovingHinge2; }
                        break;

                    case RearDockStates.MovingHinge2:
                        if (MoveHinge2()) { CurrentRearDockState = RearDockStates.Moved; }
                        break;

                    case RearDockStates.Moved:
                        // do nothing
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
                        if (ResetHinge2())
                        {
                            switch (DestinationAfterNeutral)
                            {
                                case RearDockStates.Moved:
                                    CurrentRearDockState = RearDockStates.Moving;
                                    break;
                                default:
                                    CurrentRearDockState = RearDockStates.Frozen;
                                    break;
                            }
                        }
                        break;

                    case RearDockStates.Frozen: 
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
                ActuatorPosition currentPositions = new ActuatorPosition
                {
                    PistonDistance = RearDockPiston.CurrentPosition,
                    Hinge1Angle = RearDockHinge1.Angle,
                    Hinge2Angle = RearDockHinge2.Angle,
                };
                foreach (KeyValuePair<string, ActuatorPosition> port in KnownPorts)
                {
                    if (
                        Math.Abs(currentPositions.PistonDistance - port.Value.PistonDistance) < PISTON_POSITION_TOLERANCE && 
                        Math.Abs(currentPositions.Hinge1Angle - port.Value.Hinge1Angle) < HINGE_ANGLE_TOLERANCE && 
                        Math.Abs(currentPositions.Hinge2Angle - port.Value.Hinge2Angle) < HINGE_ANGLE_TOLERANCE)
                    {
                        return RearDockStates.Moved;
                    }
                }
                return RearDockStates.Unknown;
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

            private bool ResetHinge1() // get hinge 1 to 0 degrees (pointing straight back)
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

            private bool ResetHinge2() // get hinge 2 to 0 degrees.
            {
                RearDockHinge2.Torque = HINGE_TORQUE;
                if (Math.Abs(RearDockHinge2.Angle - Math.PI / 2) < HINGE_ANGLE_TOLERANCE)
                {
                    RearDockHinge2.TargetVelocityRPM = 0;
                    return true;
                }
                else if (HINGE_ANGLE_TOLERANCE - RearDockHinge2.Angle < 0)
                {
                    RearDockHinge2.UpperLimitDeg = 0;
                    RearDockHinge2.TargetVelocityRPM = HINGE_TARGET_VELOCITY;
                    return false;
                }
                else if (HINGE_ANGLE_TOLERANCE - RearDockHinge2.Angle > 0)
                {
                    RearDockHinge2.LowerLimitDeg = 0;
                    RearDockHinge2.TargetVelocityRPM = -HINGE_TARGET_VELOCITY;
                    return false;
                }
                else return false;
            }

            private bool NeutralToRetracted()
            {
                RearDockHinge2.LowerLimitDeg = -90;
                RearDockHinge2.TargetVelocityRPM = -HINGE_TARGET_VELOCITY;

                RearDockPiston.MinLimit = 0;
                RearDockPiston.Velocity = -PISTON_TARGET_VELOCITY;
                return Math.Abs(RearDockHinge2.Angle - Math.PI / 2) < HINGE_ANGLE_TOLERANCE && RearDockPiston.CurrentPosition < PISTON_POSITION_TOLERANCE;
            }

            private void SetUpExtension(ActuatorPosition portParameters)
            {
                PistonTargetDistance = portParameters.PistonDistance;
                Hinge1TargetAngle = portParameters.Hinge1Angle;
                Hinge2TargetAngle = portParameters.Hinge2Angle;
            }

            private bool MovePiston()
            {
                if (RearDockPiston.CurrentPosition - PistonTargetDistance < PISTON_POSITION_TOLERANCE)
                {
                    RearDockPiston.Velocity = 0;
                    return true;
                }
                else if (RearDockPiston.CurrentPosition < PistonTargetDistance)
                {
                    RearDockPiston.MaxLimit = PistonTargetDistance;
                    RearDockPiston.Velocity = PISTON_TARGET_VELOCITY;
                    return false;
                }
                else if (RearDockPiston.CurrentPosition > PistonTargetDistance)
                {
                    RearDockPiston.MinLimit = PistonTargetDistance;
                    RearDockPiston.Velocity = -PISTON_TARGET_VELOCITY;
                    return false;
                }
                else return false;
            }

            private bool MoveHinge1()
            {
                RearDockHinge1.Torque = HINGE_TORQUE;
                if (Math.Abs(RearDockHinge1.Angle - Hinge1TargetAngle) < HINGE_ANGLE_TOLERANCE)
                {
                    RearDockHinge1.TargetVelocityRPM = 0;
                    return true;
                }
                else if (RearDockHinge1.Angle < Hinge1TargetAngle)
                {
                    RearDockHinge1.UpperLimitRad = Hinge1TargetAngle;
                    RearDockHinge1.TargetVelocityRPM = HINGE_TARGET_VELOCITY;
                    return false;
                }
                else if (RearDockHinge1.Angle > Hinge1TargetAngle)
                {
                    RearDockHinge1.LowerLimitRad = Hinge1TargetAngle;
                    RearDockHinge1.TargetVelocityRPM = -HINGE_TARGET_VELOCITY;
                    return false;
                }
                else return false;
            }

            private bool MoveHinge2()
            {
                RearDockHinge2.Torque = HINGE_TORQUE;
                if (Math.Abs(RearDockHinge2.Angle - Hinge2TargetAngle) < HINGE_ANGLE_TOLERANCE)
                {
                    RearDockHinge2.TargetVelocityRPM = 0;
                    return true;
                }
                else if (RearDockHinge2.Angle < Hinge2TargetAngle)
                {
                    RearDockHinge2.UpperLimitRad = Hinge2TargetAngle;
                    RearDockHinge2.TargetVelocityRPM = HINGE_TARGET_VELOCITY;
                    return false;
                }
                else if (RearDockHinge2.Angle > Hinge2TargetAngle)
                {
                    RearDockHinge2.LowerLimitRad = Hinge2TargetAngle;
                    RearDockHinge2.TargetVelocityRPM = -HINGE_TARGET_VELOCITY;
                    return false;
                }
                else return false;
            }


        }
    }
}
