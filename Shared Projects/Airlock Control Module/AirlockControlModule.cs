﻿using Sandbox.Game.Entities.Cube;
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
            public List<Airlock> Airlocks = new List<Airlock>();

            public void LoadAirlocks(IMyGridTerminalSystem grid, IMyProgrammableBlock programmableBlock)
            {
                // create a temporary list of all the doors on the grid
                List<IMyTerminalBlock> allDoors = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyDoor>(allDoors, airlock => airlock is IMyDoor && airlock.CubeGrid == programmableBlock.CubeGrid);
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
                            airlock.StateMachine = new AirlockStateMachine(door, partner);
                            Airlocks.Add(airlock);
                        }
                    }
                    // remove both doors from the temp list
                    allDoors.Remove(door);
                    allDoors.Remove((IMyTerminalBlock)grid.GetBlockWithName(customData));
                }
            }

            public string GetAirlockPairs()
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
                foreach (Airlock airlock in Airlocks)
                {
                    airlock.StateMachine.Update();
                }
            }
        }
    }
}
