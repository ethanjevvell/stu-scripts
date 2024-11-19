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
            public const float HINGE_ANGLE_TOLERANCE = 0.0071f;
            public const float HINGE_TARGET_VELOCITY = 1f;
            public const float HINGE_TORQUE = 7000000;
            public const float PISTON_POSITION_TOLERANCE = 0.01f;
            public const float PISTON_TARGET_VELOCITY = 1.4f;
            public const float PISTON_NEUTRAL_DISTANCE = 4f;
            public static IMyPistonBase RearDockPiston { get; set; }
            public static IMyMotorStator RearDockHinge1 { get; set; }
            public static IMyMotorStator RearDockHinge2 { get; set; }
            public static IMyShipConnector RearDockConnector { get; set; }
            private float PistonTargetDistance { get; set; }
            private float Hinge1TargetAngle { get; set; }
            private float Hinge2TargetAngle { get; set; }

            public enum RearDockStates
            {
                Idle,
                Moving,
            }

            public struct ActuatorPosition
            {
                public float PistonDistance;
                public float Hinge1Angle;
                public float Hinge2Angle;
            }
            public static ActuatorPosition CurrentPosition { get; set; }
            public static int DesiredPosition { get; set; }

            public static ActuatorPosition[] KnownPorts = new ActuatorPosition[]
            {
                new ActuatorPosition { PistonDistance = 0, Hinge1Angle = DegToRad(90), Hinge2Angle = DegToRad(90) }, // stowed
                new ActuatorPosition { PistonDistance = 4, Hinge1Angle = 0, Hinge2Angle = 0 }, // neutral
                new ActuatorPosition { PistonDistance = 10, Hinge1Angle = DegToRad(36), Hinge2Angle = DegToRad(-36) }, // lunar hq
                new ActuatorPosition { PistonDistance = 3.5f, Hinge1Angle = DegToRad(-90), Hinge2Angle = DegToRad(-72) }, // herobrine on deck
            };

            public RearDockStates CurrentRearDockState { get; set; } 
            public static Queue<string> ManeuverQueue { get; set; } = new Queue<string>();
            public static STUStateMachine CurrentManeuver { get; set; }

            public static float DegToRad(float degrees)
            {
                return degrees * (float)(Math.PI / 180);
            }

            //constructor
            public CBTRearDock(IMyPistonBase piston, IMyMotorStator hinge1, IMyMotorStator hinge2, IMyShipConnector connector)
            {
                RearDockPiston = piston;
                RearDockHinge1 = hinge1;
                RearDockHinge2 = hinge2;
                RearDockConnector = connector;

                RearDockHinge1.BrakingTorque = HINGE_TORQUE;
                RearDockHinge2.BrakingTorque = HINGE_TORQUE;
            }

            // state machine
            public void UpdateRearDock()
            {
                // see if the user input position is different from the internal position variable
                // if it is, then queue up that new position
                // ideally, interrupt the current maneuver and start the new one
                if (CBT.UserInputRearDockPosition != InternalPositionTarget)
                {
                    if (KnownPorts.ContainsKey(InternalPositionTarget))
                    {
                        ManeuverQueue.Enqueue(KnownPorts[InternalPositionTarget].ToString());
                    }
                }
                
                switch (CurrentRearDockState)
                {
                    case RearDockStates.Idle:
                        // check for work
                        if (ManeuverQueue.Count > 0)
                        {
                            try
                            {
                                CurrentManeuver = ManeuverQueue.Dequeue();

                            }
                            catch
                            {

                            }
                        }
                    case RearDockStates.Moving:
                        if (CurrentManeuver.ExecuteStateMachine())
                        {
                            CurrentManeuver = null;
                            CurrentRearDockState = RearDockStates.Idle;
                        }
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
                        return RearDockStates.Idle;
                    }
                }
                throw new Exception();
            }

            public bool CanGoToRequestedState(RearDockStates requestedState)
            {
                CBT.AddToLogQueue($"asking whether we can go to state {requestedState} from state {CurrentRearDockState}");
                if (ValidStateTransitions[CurrentRearDockState].Contains(requestedState)) { return true; }
                else return false;
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

            private bool MoveHinge(IMyMotorStator hinge)
            {
                hinge.Torque = HINGE_TORQUE;
                if (Math.Abs(hinge.Angle - Hinge1TargetAngle) < HINGE_ANGLE_TOLERANCE)
                {
                    hinge.TargetVelocityRPM = 0;
                    return true;
                }
                else if (hinge.Angle < Hinge1TargetAngle)
                {
                    hinge.UpperLimitRad = Hinge1TargetAngle;
                    hinge.TargetVelocityRPM = HINGE_TARGET_VELOCITY;
                    return false;
                }
                else if (hinge.Angle > Hinge1TargetAngle)
                {
                    hinge.LowerLimitRad = Hinge1TargetAngle;
                    hinge.TargetVelocityRPM = -HINGE_TARGET_VELOCITY;
                    return false;
                }
                else return false;
            }

        }
    }
}
