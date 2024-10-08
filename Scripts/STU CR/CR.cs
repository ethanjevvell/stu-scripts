using Sandbox.Game.Screens.DebugScreens;
using Sandbox.ModAPI.Ingame;
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
        public partial class CR
        {
            public static IMyGridTerminalSystem CRGrid;
            public static List<IMyTerminalBlock> CRBlocks = new List<IMyTerminalBlock>();
            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static IMyGridProgramRuntimeInfo Runtime { get; set; }
            public static Action<string> Echo { get; set; }

            public Vector3D CurrentPosition;

            public static IMyShipMergeBlock MergeBlock { get; set; }

            public CR(Action<string> echo, STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime)
            {
                Me = me;
                Broadcaster = broadcaster;
                Runtime = runtime;
                CRGrid = grid;
                Echo = echo;

                LoadMergeBlock(grid);
            }

            public void LoadMergeBlock(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> mergeBlock = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyShipMergeBlock>(mergeBlock, block => block.CubeGrid == Me.CubeGrid);
                if (mergeBlock.Count == 0)
                {
                    Echo("No merge block found");
                    return;
                }
                else if (mergeBlock.Count > 1)
                {
                    Echo("Multiple merge blocks found, assigning first one found...");
                }
                MergeBlock = mergeBlock[0] as IMyShipMergeBlock;
            }

            public void UpdatePBCurrentPosition()
            {
                CurrentPosition = Me.GetPosition();
            }
        }
    }
    
}
