using Sandbox.Game.Screens.DebugScreens;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        

        public partial class BALLS
        {
            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static STUInventoryEnumerator InventoryEnumerator { get; set; }
            public static IMyGridProgramRuntimeInfo Runtime { get; set; }
            public static IMyGridTerminalSystem BALLSGrid { get; set; }
            public static Action<string> Echo { get; set; }

            public static IMyMotorAdvancedStator MainRotor { get; set; }
            public static IMyProjector Projector { get; set; }
            public static IMyShipConnector Connector { get; set; }
            public static IMyShipMergeBlock MergeBlock { get; set; }
            public static IMyShipWelder[] Welders { get; set; }
            public BALLS(Action<string> echo, STUMasterLogBroadcaster broadcaster, STUInventoryEnumerator inventoryEnumerator, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime)
            {
                Me = me;
                Broadcaster = broadcaster;
                InventoryEnumerator = inventoryEnumerator;
                Runtime = runtime;
                BALLSGrid = grid;
                Echo = echo;

                LoadMainRotor(grid);
                LoadProjector(grid);
                LoadConnector(grid);
                LoadMergeBlock(grid);
                LoadWelders(grid);
            }

            #region Hardware Initialization

            public void LoadMainRotor(IMyGridTerminalSystem grid)
            {
                MainRotor = grid.GetBlockWithName("Main Rotor") as IMyMotorAdvancedStator;
                if (MainRotor == null)
                {
                    Echo("Main Rotor not found!");
                    return;
                }
            }

            public void LoadProjector(IMyGridTerminalSystem grid)
            {
                Projector = grid.GetBlockWithName("Projector") as IMyProjector;
                if (Projector == null)
                {
                    Echo("Projector not found!");
                    return;
                }
            }

            public void LoadConnector(IMyGridTerminalSystem grid)
            {
                Connector = grid.GetBlockWithName("Connector") as IMyShipConnector;
                if (Connector == null)
                {
                    Echo("Connector not found!");
                    return;
                }
            }

            public void LoadMergeBlock(IMyGridTerminalSystem grid)
            {
                MergeBlock = grid.GetBlockWithName("Merge Block") as IMyShipMergeBlock;
                if (MergeBlock == null)
                {
                    Echo("Merge Block not found!");
                    return;
                }
            }

            public void LoadWelders(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> welders = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyShipWelder>(welders, block => block.CubeGrid == Me.CubeGrid);
                if (welders.Count == 0)
                {
                    Echo("Welders not found!");
                    return;
                }

                IMyShipWelder[] welderArray = new IMyShipWelder[welders.Count];
                for (int i = 0; i < welders.Count; i++)
                {
                    welderArray[i] = welders[i] as IMyShipWelder;
                }

                Welders = welderArray;
            }

            #endregion Hardware Initialization
        }
    }
}
