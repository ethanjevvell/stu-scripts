using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
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
using VRage.Game.ObjectBuilders;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        
        public List<IMyRefinery> refineries = new List<IMyRefinery>();
        public List<IMyAssembler> assemblers = new List<IMyAssembler>();
        public List<IMyTerminalBlock> inventories = new List<IMyTerminalBlock>();

        public IMyProjector projector;



        public Program()
        {

            GridTerminalSystem.GetBlocksOfType(refineries);
            GridTerminalSystem.GetBlocksOfType(assemblers);
            GridTerminalSystem.GetBlocksOfType(inventories, inventory => inventory.HasInventory);
            projector = GridTerminalSystem.GetBlockWithName("Build Dock Projector") as IMyProjector;
            List<IMyTerminalBlock> inventories = new List<IMyTerminalBlock>();

        }


        public void Main(string argument, UpdateType updateSource)
        {
            
        }

        
    }
}
