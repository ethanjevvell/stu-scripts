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
            public static IMyMotorStator GangwayHinge { get; set; }
            public static IMyMotorStator MainDockHinge1 { get; set; }
            public static IMyMotorStator MainDockHinge2 { get; set; }
            public static IMyPistonBase MainDockPiston { get; set; }
            public static IMyShipConnector MainDockConnector { get; set; }

            public static CRDockingModule DockingModule { get; set;}
            public static AirlockControlModule ACM { get; set; }

            public CR(Action<string> echo, STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime)
            {
                Me = me;
                Broadcaster = broadcaster;
                Runtime = runtime;
                CRGrid = grid;
                Echo = echo;

                AddLogSubscribers(grid);

                LoadGangwayHinge(grid);
                LoadMainDockHinge1(grid);
                LoadMainDockHinge2(grid);
                LoadMainDockPiston(grid);
                LoadMainDockConnector(grid);
                LoadMergeBlock(grid);

                DockingModule = new CRDockingModule(GangwayHinge, MainDockHinge1, MainDockHinge2, MainDockPiston, MergeBlock, MainDockConnector);
                ACM = new AirlockControlModule();
                ACM.LoadAirlocks(grid, me);

                AddToLogQueue("CR Initialized", STULogType.INFO);
                echo("CR Initialized");
            }

            #region High-Level Software Control Methods
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
            #endregion High-Level Software Control Methods

            #region Screen Update Methods
            public static void UpdateLogScreens()
            {
                foreach (var screen in LogChannel)
                {
                    screen.StartFrame();
                    screen.WriteWrappableLogs(screen.FlightLogs);
                    screen.EndAndPaintFrame();
                }
            }
            #endregion Screen Update Methods

            #region Hardware Initialization
            #region Screens
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
            #endregion Screens

            #region Other
            public void LoadGangwayHinge(IMyGridTerminalSystem grid)
            {
                var hinge = grid.GetBlockWithName("CR Gangway Hinge") as IMyMotorStator;
                if (hinge == null)
                {
                    AddToLogQueue("Could not find CR Gangway Hinge; ensure it is named properly.", STULogType.ERROR);
                    return;
                }
                GangwayHinge = hinge;
                AddToLogQueue("Gangway hinge ... loaded", STULogType.INFO);
            }

            public void LoadMainDockHinge1(IMyGridTerminalSystem grid)
            {
                var hinge = grid.GetBlockWithName("CR Main Dock Hinge 1") as IMyMotorStator;
                if (hinge == null)
                {
                    AddToLogQueue("Could not find CR Main Dock Hinge 1; ensure it is named properly.", STULogType.ERROR);
                    return;
                }
                MainDockHinge1 = hinge;
                AddToLogQueue("Main Dock Hinge 1 ... loaded", STULogType.INFO);
            }

            public void LoadMainDockHinge2(IMyGridTerminalSystem grid)
            {
                var hinge = grid.GetBlockWithName("CR Main Dock Hinge 2") as IMyMotorStator;
                if (hinge == null)
                {
                    AddToLogQueue("Could not find CR Main Dock Hinge 2; ensure it is named properly.", STULogType.ERROR);
                    return;
                }
                MainDockHinge2 = hinge;
                AddToLogQueue("Main Dock Hinge 2 ... loaded", STULogType.INFO);
            }

            public void LoadMainDockPiston(IMyGridTerminalSystem grid)
            {
                var piston = grid.GetBlockWithName("CR Main Dock Piston") as IMyPistonBase;
                if (piston == null)
                {
                    AddToLogQueue("Could not find CR Main Dock Piston; ensure it is named properly.", STULogType.ERROR);
                    return;
                }
                MainDockPiston = piston;
                AddToLogQueue("Main Dock Piston ... loaded", STULogType.INFO);
            }

            public void LoadMainDockConnector(IMyGridTerminalSystem grid)
            {
                var connector = grid.GetBlockWithName("CR Main Dock Connector") as IMyShipConnector;
                if (connector == null)
                {
                    AddToLogQueue("Could not find CR Main Dock Connector; ensure it is named properly.", STULogType.ERROR);
                    return;
                }
                MainDockConnector = connector;
                MainDockConnector.Enabled = true;
                MainDockConnector.IsParkingEnabled = false;
                MainDockConnector.PullStrength = 0;
                AddToLogQueue("Main Dock Connector ... loaded", STULogType.INFO);
            }

            public void LoadMergeBlock(IMyGridTerminalSystem grid)
            {
                var mergeBlock = grid.GetBlockWithName("CR Merge Block") as IMyShipMergeBlock;
                if (mergeBlock == null)
                {
                    AddToLogQueue("Could not find CR Merge Block; ensure it is named properly.", STULogType.ERROR);
                    return;
                }
                MergeBlock = mergeBlock;
                AddToLogQueue("Merge Block ... loaded", STULogType.INFO);
            }
            #endregion Other
            #endregion Hardware Initialization

            // CR Helper Methods
            #region CR Helper Methods
            public void UpdatePBCurrentPosition()
            {
                CurrentPosition = Me.GetPosition();
            }
            #endregion CR Helper Methods
        }
    }
    
}
