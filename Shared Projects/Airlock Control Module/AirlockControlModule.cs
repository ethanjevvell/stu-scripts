using Sandbox.Game.Entities.Cube;
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
        
        public class AirlockControlModule
        {
            
            public struct Airlock
            {
                public IMyDoor SideA;
                public IMyDoor SideB;
                public AirlockStateMachine StateMachine;
            }
            public struct SoloAirlock
            {
                public IMyDoor Door;
                public SoloAirlockStateMachine StateMachine;
            }
            public List<Airlock> Airlocks = new List<Airlock>();
            public List<SoloAirlock> SoloAirlocks = new List<SoloAirlock>();

            public bool Enabled { get; set; } = true;

            public void LoadAirlocks(IMyGridTerminalSystem grid, IMyProgrammableBlock programmableBlock, IMyGridProgramRuntimeInfo runtime)
            {
                // create a temporary list of all the doors on the grid
                List<IMyTerminalBlock> allDoors = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyDoor>(allDoors, airlock => airlock is IMyDoor && airlock.CubeGrid == programmableBlock.CubeGrid);
                // extract all the solo airlocks from the temp list by checking whether their custom data contains "solo"
                for (int i = 0; i < allDoors.Count; i++)
                {
                    IMyDoor door = (IMyDoor)allDoors[i];
                    if (door.CustomData.ToUpper().Contains("SOLO"))
                    {
                        SoloAirlock soloAirlock = new SoloAirlock();
                        soloAirlock.Door = door;
                        soloAirlock.StateMachine = new SoloAirlockStateMachine(door, runtime);
                        SoloAirlocks.Add(soloAirlock);
                        allDoors.Remove(door);
                        i--;
                    }
                }
                // loop through the temp list, // repeat until the temp list is empty.
                while (allDoors.Count > 0)
                {
                    IMyDoor door = (IMyDoor)allDoors[0];
                    // get its custom data.
                    string customData = door.CustomData;
                    // if it has a partner, add both doors to a new Airlock struct and add that to the Airlocks list
                    if (customData != "")
                    {
                        IMyDoor partner = (IMyDoor)grid.GetBlockWithName(customData);
                        if (partner != null)
                        {
                            Airlock airlock = new Airlock();
                            airlock.SideA = door;
                            airlock.SideB = partner;
                            airlock.StateMachine = new AirlockStateMachine(door, partner, runtime);
                            Airlocks.Add(airlock);
                        }
                    }
                    // remove both doors from the temp list
                    allDoors.Remove(door);
                    allDoors.Remove((IMyTerminalBlock)grid.GetBlockWithName(customData));
                }
            }

            public string GetAirlocks()
            {
                string result = "Airlocks:\n\n";
                foreach (Airlock airlock in Airlocks)
                {
                    result += $"{airlock.SideA.CustomName} <-> {airlock.SideB.CustomName}\n";
                }
                return result;
            }

            public void UpdateAirlocks()
            {
                if (!Enabled) return;
                
                foreach (Airlock airlock in Airlocks)
                {
                    airlock.StateMachine.Update();
                }
                foreach (SoloAirlock soloAirlock in SoloAirlocks)
                {
                    soloAirlock.StateMachine.Update();
                }
                
            }

            public void DisableAuomaticControl()
            {
                Enabled = false;
            }

            public void EnableAutomaticControl()
            {
                Enabled = true;
            }
        }
    }
}
