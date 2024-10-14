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
            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static IMyGridProgramRuntimeInfo Runtime { get; set; }
            public static Action<string> Echo { get; set; }

            public static IMyGridTerminalSystem CRGrid;
            public static List<IMyTerminalBlock> CRBlocks = new List<IMyTerminalBlock>();
            public static List<CRLogLCD> LogChannel = new List<CRLogLCD>();

            public Vector3D CurrentPosition;

            public static IMyShipMergeBlock MergeBlock { get; set; }

            public CR(Action<string> echo, STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime)
            {
                Me = me;
                Broadcaster = broadcaster;
                Runtime = runtime;
                CRGrid = grid;
                Echo = echo;

                // LoadMergeBlock(grid);
                AddLogSubscribers(grid);

                AddToLogQueue("CR Initialized", STULogType.INFO);
                echo("CR Initialized");
            }

            public static void CreateBroadcast(string message, bool encrypt = false, string type = STULogType.INFO)
            {
                string key = null;
                if (encrypt)
                    key = CBT_VARIABLES.TEA_KEY;

                Broadcaster.Log(new STULog
                {
                    Sender = CBT_VARIABLES.CBT_VEHICLE_NAME,
                    Message = message,
                    Type = type,
                });
            }
            public static void AddToLogQueue(string message, string type = STULogType.INFO, string sender = CBT_VARIABLES.CBT_VEHICLE_NAME)
            {
                foreach (var screen in LogChannel)
                {
                    screen.FlightLogs.Enqueue(new STULog
                    {
                        Sender = sender,
                        Message = message,
                        Type = type,
                    });
                }
            }
            public static void UpdateLogScreens()
            {
                foreach (var screen in LogChannel)
                {
                    screen.StartFrame();
                    screen.WriteWrappableLogs(screen.FlightLogs);
                    screen.EndAndPaintFrame();
                }
            }

            private static void AddLogSubscribers(IMyGridTerminalSystem grid)
            {
                grid.GetBlocks(CRBlocks);
                foreach (var block in CRBlocks)
                {
                    string CustomDataRawText = block.CustomData;
                    string[] CustomDataLines = CustomDataRawText.Split('\n');
                    foreach (var line in CustomDataLines)
                    {
                        if (line.Contains("CR_LOG"))
                        {
                            string[] kvp = line.Split(':');
                            // adjust font size based on what screen we're trying to initalize
                            float fontSize;
                            try
                            {
                                fontSize = float.Parse(kvp[2]);
                                if (fontSize < 0.1f || fontSize > 10f)
                                {
                                    throw new Exception("Invalid font size");
                                }
                            }
                            catch (Exception e)
                            {
                                Echo("caught exception in AddLogSubscribers:");
                                Echo(e.Message);
                                fontSize = 0.5f;
                            }
                            CRLogLCD screen = new CRLogLCD(Echo, block, int.Parse(kvp[1]), "Monospace", fontSize);
                            LogChannel.Add(screen);
                        }
                    }
                }
            }

            // currently broken??
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
