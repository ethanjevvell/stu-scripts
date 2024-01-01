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

            Action<string> Echo;
            IMyGridTerminalSystem GridTerminalSystem;
            int NumDistanceLights;

            public DockActuatorGroup(string name, IMyMotorStator hinge1, IMyPistonBase piston,IMyMotorStator hinge2, IMyShipConnector connector, List<IMyInteriorLight> distanceLights, Action<string> echo, IMyGridTerminalSystem grid, int numDistanceLights)
            {
                Name = name;
                Hinge1 = hinge1;
                Piston = piston;
                Hinge2 = hinge2;
                Connector = connector;
                DistanceLights = distanceLights;
                Echo = echo;
                GridTerminalSystem = grid;
                NumDistanceLights = numDistanceLights;
            }

            public string Name { get; set; }

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
                Disconnect(Connector);
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

            public void ActivateRunwayLight(string dock, int lightIndex)
            {
                // turn off all runway lights of a given runway
                for (int i = 1; i <= NumDistanceLights; i++)
                {
                    string strI = i.ToString();
                    if (i < 10) { strI = $"0{i.ToString()}"; };
                    IMyInteriorLight light = GridTerminalSystem.GetBlockWithName($"{dock} Dock Distance Light {strI}") as IMyInteriorLight;
                    light.Enabled = false;
                }

                // turn on just the landing light of a given runway
                Echo($"check 2");
                if (lightIndex == 0) { return; }
                else
                {
                    string strLightIndex = lightIndex.ToString();
                    Echo($"{lightIndex}");
                    if (lightIndex < 10) { strLightIndex = $"0{lightIndex.ToString()}"; };
                    Echo($"{dock}\n{lightIndex}\n{strLightIndex}");
                    IMyInteriorLight landingLight = GridTerminalSystem.GetBlockWithName($"{dock} Dock Distance Light {lightIndex}") as IMyInteriorLight;
                    landingLight.Enabled = true;
                    landingLight.Color = Color.White;
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
