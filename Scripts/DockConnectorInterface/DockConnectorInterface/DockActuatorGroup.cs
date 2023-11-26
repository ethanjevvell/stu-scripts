using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        
        public class DockActuatorGroup
        {
            IMyMotorStator Hinge1;
            IMyPistonBase Piston;
            IMyMotorStator Hinge2;
            IMyShipConnector Connector;

            public float currentHingePosition;
            public float currentHingeVelocity;

            public float currentPistonPosition;

            public float hinge1angle;
            public float pistonDistance;
            public float hinge2angle;

            Action<string> Echo;

            public DockActuatorGroup(IMyMotorStator hinge1, IMyPistonBase piston,IMyMotorStator hinge2, IMyShipConnector connector, Action<string> echo)
            {
                Hinge1 = hinge1;
                Piston = piston;
                Hinge2 = hinge2;
                Connector = connector;

                Echo = echo;
            }

            public void Move(float hinge1position, float pistonPosition, float  hinge2position)
            {
                MovePiston(Piston, 0);
                MoveHinge(Hinge1, hinge1position);
                MoveHinge(Hinge2, hinge2position);
                MovePiston(Piston, pistonPosition);
            }

            public void MoveHinge(IMyMotorStator hinge, float targetPosition)
            {
                // gather information about the hinge, convert radians to degrees while we're at it
                currentHingePosition = (float)Math.Round((double)hinge.Angle * 180 / Math.PI);
                currentHingeVelocity = hinge.TargetVelocityRPM;

                // determine if the hinge can be moved, return if else
                if (hinge.IsWorking == false || currentHingeVelocity == 0)
                {
                    Echo($"Hinge cannot be moved");
                    return;
                }

                // logic to keep the targetPosition within the bounds of what is allowable
                if (targetPosition > 90) { targetPosition = 90; };
                if (targetPosition < -90) { targetPosition = -90; };

                // lock hinge for safety.
                float oldBrakingTorque = hinge.BrakingTorque;
                hinge.BrakingTorque = hinge.Torque;
                hinge.RotorLock = true;

                // set the hinge limits to the extremes
                hinge.UpperLimitDeg = 90;
                hinge.LowerLimitDeg = -90;

                // if targetPosition is larger than rounded currentHingePosition
                // set upper limit to targetPosition and set velocity to positive
                // if targetPosition is smaller than currentHingePosition
                // set lower limit to targetPosition and set velocity to negative
                if (targetPosition > currentHingePosition)
                {
                    hinge.UpperLimitDeg = targetPosition;
                    hinge.TargetVelocityRPM = (float)Math.Abs((decimal)hinge.TargetVelocityRPM);
                }
                if (targetPosition < currentHingePosition)
                {
                    hinge.LowerLimitDeg = targetPosition;
                    hinge.TargetVelocityRPM = (float)Math.Abs((decimal)hinge.TargetVelocityRPM) * -1;
                }

                // unlock the hinge
                hinge.BrakingTorque = oldBrakingTorque;
                hinge.RotorLock = false;
            }

            public void MovePiston(IMyPistonBase piston, float targetDistance)
            {
                // gather information about the piston
                currentPistonPosition = (float)Math.Round(piston.CurrentPosition);

                // determine if piston can be moved, return if else
                if (piston.IsWorking == false || piston.Velocity == 0)
                {
                    Echo($"Piston cannot be moved");
                    return;
                }

                // set the limits to the extremes
                piston.MaxLimit = piston.HighestPosition;
                piston.MinLimit = piston.LowestPosition;

                // if targetDistance is greater than rounded currentPistonPosition,
                // set upper limit to targetDistance and set velocity to positive.
                // if targetDistance is smaller than rounded currentPistonPosition,
                // set lower limit to targetDistance and set velocity to negative.
                if (targetDistance > currentPistonPosition)
                {
                    piston.MaxLimit = targetDistance;
                    piston.Velocity = Math.Abs(piston.Velocity);
                }
                if (targetDistance < currentPistonPosition)
                {
                    piston.MinLimit = targetDistance;
                    piston.Velocity = Math.Abs(piston.Velocity) * -1;
                }
            }

            public void Retract(IMyShipConnector Connector)
            {
                Disconnect(Connector);
                Move(0, 0, 0);
            }

            public bool Connect(IMyShipConnector Connector)
            {
                Connector.Connect();
                return Connector.IsConnected;
            }

            public void Disconnect(IMyShipConnector Connector) 
            {
                Connector.Disconnect();
            }


        }
    }
}
