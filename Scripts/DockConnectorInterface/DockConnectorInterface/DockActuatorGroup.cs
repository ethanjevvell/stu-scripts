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
            public IMyMotorStator Hinge1;
            public IMyPistonBase Piston;
            public IMyMotorStator Hinge2;
            public IMyShipConnector Connector;
            public List<IMyInteriorLight> DistanceLights;

            public float currentHingePosition;
            public float currentHingeVelocity;

            public float currentPistonPosition;

            public float hinge1angle;
            public float pistonDistance;
            public float hinge2angle;

            public float lastHinge1Target = 0;
            public float lastPistonTarget = 0;
            public float lastHinge2Target = 0;
            public List<float> lastTargetPosition = new List<float>();

            public string name;

            Action<string> Echo;

            public DockActuatorGroup(IMyMotorStator hinge1, IMyPistonBase piston,IMyMotorStator hinge2, IMyShipConnector connector, List<IMyInteriorLight> distanceLights)
            {
                Hinge1 = hinge1;
                Piston = piston;
                Hinge2 = hinge2;
                Connector = connector;
                DistanceLights = distanceLights;
            }

            public bool IsStationary()
            {
                if (
                    Hinge1.TargetVelocityRPM < 0.1 &&
                    Piston.Velocity < 0.1 &&
                    Hinge2.TargetVelocityRPM < 0.1
                    ) 
                    return true;

                else return false;
            }

            public void Move(float hinge1TargetPosition, float pistonTargetPosition, float  hinge2TargetPosition)
            {
                lastHinge1Target = hinge1TargetPosition;
                lastPistonTarget = pistonTargetPosition;
                lastHinge2Target = hinge2TargetPosition;

                MovePiston(Piston, 0);
                MoveHinge(Hinge1, hinge1TargetPosition);
                MoveHinge(Hinge2, hinge2TargetPosition);
                MovePiston(Piston, pistonTargetPosition);

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

                // had to remove the logic that locks the hinges because I realized
                // that this code block runs basically instantaneously due to the FSM construction.

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

            public void IlluminateLight(List<IMyInteriorLight> distanceLights, int lightIndex)
            {
                foreach (var light in distanceLights)
                {
                    light.Enabled = false;
                }

                distanceLights[lightIndex].Enabled = true;
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

            public List<float> GetTargetPosition()
            {
                lastTargetPosition[0] = lastHinge1Target;
                lastTargetPosition[1] = lastPistonTarget;
                lastTargetPosition[2] = lastHinge2Target;

                return lastTargetPosition;
            }
        }
    }
}
