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
            public const float HINGE_ANGLE_TOLERANCE = 0.0077f;
            public const float HINGE_TARGET_VELOCITY = 4f;
            public const float HINGE_TORQUE = 7000000;
            public const float PISTON_POSITION_TOLERANCE = 0.01f;
            public const float PISTON_TARGET_VELOCITY = 1.7f;
            public const float PISTON_NEUTRAL_DISTANCE = 4f;
            public static IMyPistonBase RearDockPiston { get; set; }
            public static IMyMotorStator RearDockHinge1 { get; set; }
            public static IMyMotorStator RearDockHinge2 { get; set; }
            public static IMyShipConnector RearDockConnector { get; set; }

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
            public static int DesiredPosition { get; set; }

            public static ActuatorPosition[] KnownPorts = new ActuatorPosition[]
            {
                new ActuatorPosition { PistonDistance = 0, Hinge1Angle = DegToRad(90), Hinge2Angle = DegToRad(90) }, // stowed
                new ActuatorPosition { PistonDistance = 4, Hinge1Angle = 0, Hinge2Angle = 0 }, // neutral
                new ActuatorPosition { PistonDistance = 10, Hinge1Angle = DegToRad(36), Hinge2Angle = DegToRad(-36) }, // lunar hq
                new ActuatorPosition { PistonDistance = 3.5f, Hinge1Angle = DegToRad(-90), Hinge2Angle = DegToRad(-72) }, // herobrine on deck
                new ActuatorPosition { PistonDistance = 10f, Hinge1Angle = DegToRad(81), Hinge2Angle = DegToRad(9) }, // CLAM
            };

            public RearDockStates CurrentRearDockPhase { get; set; } 
            public static Queue<STUStateMachine> ManeuverQueue { get; set; } = new Queue<STUStateMachine>();
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

            public void BuildManeuver()
            {
                ManeuverQueue.Enqueue(new CBT.MovePiston(RearDockPiston, PISTON_NEUTRAL_DISTANCE));
                ManeuverQueue.Enqueue(new CBT.MoveHinge(RearDockHinge1, KnownPorts[DesiredPosition].Hinge1Angle));
                ManeuverQueue.Enqueue(new CBT.MoveHinge(RearDockHinge2, KnownPorts[DesiredPosition].Hinge2Angle));
                ManeuverQueue.Enqueue(new CBT.MovePiston(RearDockPiston, KnownPorts[DesiredPosition].PistonDistance));
            }

            // state machine
            public void UpdateRearDock()
            {
                // see if the user input position is different from the internal position variable
                // if it is, then queue up that new position
                // ideally, interrupt the current maneuver and start the new one
                if (CBT.UserInputRearDockPosition != DesiredPosition)
                {
                    try
                    {
                        ManeuverQueue.Clear();
                        DesiredPosition = CBT.UserInputRearDockPosition;
                        BuildManeuver();
                    }
                    catch
                    {
                        CBT.AddToLogQueue("Tried to enqueue some actuator movements, but failed.");
                        ManeuverQueue.Clear();
                    }
                }

                switch (CurrentRearDockPhase)
                {
                    case RearDockStates.Idle:
                        // check for work
                        if (ManeuverQueue.Count > 0)
                        {
                            try
                            {
                                CurrentManeuver = ManeuverQueue.Dequeue();
                                CurrentRearDockPhase = RearDockStates.Moving;
                            }
                            catch
                            {
                                CBT.AddToLogQueue("Tried to dequeue a maneuver, but failed.");
                            }
                        }
                        break;
                    case RearDockStates.Moving:
                        try
                        {
                            if (CurrentManeuver.ExecuteStateMachine())
                            {
                                CurrentManeuver = null;
                                CurrentRearDockPhase = RearDockStates.Idle;
                            }
                            break;
                        }
                        catch
                        {
                            CBT.AddToLogQueue("Current maneuver failed to execute. Likely a null value.");
                            CurrentRearDockPhase = RearDockStates.Idle;
                            break;
                        }
                    
                }
            }

        }
    }
}
