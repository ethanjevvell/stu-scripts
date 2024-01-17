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
    partial class Program : MyGridProgram
    {
        
        public List<IMyRefinery> refineries = new List<IMyRefinery>();
        public List<IMyAssembler> assemblers = new List<IMyAssembler>();

        public GridTerminalSystem.GetBlocksOfType(refineries);
        public GridTerminalSystem.GetBlocksOfType(assemblers);
        public GridTerminalSystem.GetBlocksOfType(inventories, inventory => inventory.HasInventory);

        public IMyProjector projector = GridTerminalSystem.GetBlockWithName("Build Dock Projector") as IMyProjector;

        public Program()
        {
            
            List<IMyTerminalBlock> inventories = new List<IMyTerminalBlock>();

        
            Dictionary<MyDefinitionBase, int> RemainingBlocksPerType = new Dictionary<MyDefinitionBase, int>();
        }


        public void Main(string argument, UpdateType updateSource)
        {
            
        }

        
    }
}
