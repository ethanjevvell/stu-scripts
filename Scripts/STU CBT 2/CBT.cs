using Sandbox.Game.Screens.DebugScreens;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBT
        {

            public static string LastErrorMessage = "";

            public static Action<string> echo;

            public const float TimeStep = 1.0f / 6.0f;
            public static float Timestamp = 0;
            public static Phase CurrentPhase = Phase.Idle;

            public static float UserInputForwardVelocity = 0;
            public static float UserInputRightVelocity = 0;
            public static float UserInputUpVelocity = 0;
            public static float UserInputRollVelocity = 0;
            public static float UserInputPitchVelocity = 0;
            public static float UserInputYawVelocity = 0;

            public static CBTGangway.GangwayStates UserInputGangwayState;
            public static int UserInputRearDockPosition;

            public bool CanDockWithCR = false;

            //public static Vector3D NextWaypoint;

            /// <summary>
            ///  prepare the program by declaring all the different blocks we are going to use
            /// </summary>
            // this may be potentially confusing, but "GridTerminalSystem" as it is commonly used in Program.cs to get blocks from the grid
            // does not exist in this namespace. Therefore, we are creating a new GridTerminalSystem object here to use in this class.
            // I could have named it whatever, e.g. "CBTGrid" but I don't want to have too many different names for the same thing.
            // just understand that when I reference the GridTerminalSystem property of the CBT class, I am referring to this object and NOT the one in Program.cs
            public static IMyGridTerminalSystem CBTGrid;
            public static List<IMyTerminalBlock> AllTerminalBlocks = new List<IMyTerminalBlock>();
            public static List<CBTLogLCD> LogChannel = new List<CBTLogLCD>();
            public static List<CBTAutopilotLCD> AutopilotStatusChannel = new List<CBTAutopilotLCD>();
            public static List<CBTManeuverQueueLCD> ManeuverQueueChannel = new List<CBTManeuverQueueLCD>();
            public static List<CBTAmmoLCD> AmmoChannel = new List<CBTAmmoLCD>();
            public static STUFlightController FlightController { get; set; }
            public static CBTDockingModule DockingModule { get; set; }
            public static AirlockControlModule ACM { get; set; }
            public static CBTGangway Gangway { get; set; }
            public static CBTRearDock RearDock { get; set; }
            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static STUInventoryEnumerator InventoryEnumerator { get; set; }
            public static IMyShipConnector Connector { get; set; } // fix this later, Ethan said something about the LIGMA code assuming exactly one connector
            public static IMyMotorStator RearHinge1 { get; set; }
            public static IMyMotorStator RearHinge2 { get; set; }
            public static IMyPistonBase RearPiston { get; set; }
            public static IMyMotorStator GangwayHinge1 { get; set; }
            public static IMyMotorStator GangwayHinge2 { get; set; }
            public static IMyMotorStator CameraRotor { get; set; }
            public static IMyMotorStator CameraHinge { get; set; }
            public static IMyCameraBlock Camera { get; set; }
            public static IMyRemoteControl RemoteControl { get; set; }
            public static IMyTerminalBlock FlightSeat { get; set; }
            public static STUDisplay FlightSeatFarLeftScreen { get; set; }
            public static STUDisplay FlightSeatLeftScreen { get; set; }
            public static STUDisplay PBMainScreen { get; set; }
            public static IMyGridProgramRuntimeInfo Runtime { get; set; }

            public static IMyThrust[] Thrusters { get; set; }
            public static IMyGyro[] Gyros { get; set; }
            public static IMyBatteryBlock[] Batteries { get; set; }
            public static IMyGasTank[] HydrogenTanks { get; set; }
            public static IMyLandingGear[] LandingGear { get; set; }
            public static IMyGasTank[] OxygenTanks { get; set; }
            public static IMyCryoChamber[] CryoPods { get; set; }
            public static IMyCargoContainer[] CargoContainers { get; set; }
            public static IMyShipMergeBlock MergeBlock { get; set; }
            public static IMyMedicalRoom MedicalRoom { get; set; }
            public static IMyGasGenerator[] H2O2Generators { get; set; }
            public static IMyPowerProducer[] HydrogenEngines { get; set; }
            public static IMyGravityGenerator[] GravityGenerators { get; set; }
            public static IMySensorBlock[] Sensors { get; set; }
            public static IMyInteriorLight[] InteriorLights { get; set; }
            public static IMyUserControllableGun[] GatlingTurrets { get; set; }
            public static IMyUserControllableGun[] AssaultCannons { get; set; }
            public static IMyUserControllableGun[] Railguns { get; set; }
            public static IMyButtonPanel[] ButtonPanels { get; set; }
            public static IMyShipConnector[] HangarMagPlates { get; set; }
            public static IMyDoor[] Doors { get; set; }
            public static IMyRadioAntenna Antenna { get; set; }
            public static IMyCockpit[] LowerDeckControlSeats { get; set; }
            public static IMyAssembler Assembler { get; set; }
            public static IMyRefinery Refinery { get; set; }
            public static IMyReflectorLight[] Spotlights { get; set; }
            public static IMyOreDetector OreDetector { get; set; }

            /// <summary>
            /// establish fuel and power levels
            /// 
            public static double CurrentFuel { get; set; }
            public static double CurrentPower { get; set; }
            public static double FuelCapacity { get; set; }
            public static double PowerCapacity { get; set; }

            public static bool LandingGearState { get; set; }


            // define phases for the main state machine
            // the one that will be used in conjunction with the ManeuverQueue
            public enum Phase
            {
                Idle,
                Executing,
            }

            public static Dictionary<int, List<IMyFunctionalBlock>> PowerLevelClassificationTable = new Dictionary<int, List<IMyFunctionalBlock>>()
            {
                { 0, new List<IMyFunctionalBlock> { } },
                { 1, new List<IMyFunctionalBlock> { } },
                { 2, new List<IMyFunctionalBlock> { } },
                { 3, new List<IMyFunctionalBlock> { } },
                { 4, new List<IMyFunctionalBlock> { } },
                { 5, new List<IMyFunctionalBlock> { } },
                { 6, new List<IMyFunctionalBlock> { } },
                { 7, new List<IMyFunctionalBlock> { } },
            };

            // CBT object constructor
            public CBT(Action<string> Echo, STUMasterLogBroadcaster broadcaster, STUInventoryEnumerator inventoryEnumerator, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime)
            {
                Me = me;
                Broadcaster = broadcaster;
                InventoryEnumerator = inventoryEnumerator;
                Runtime = runtime;
                CBTGrid = grid;
                echo = Echo;

                AddLogSubscribers(grid);
                LoadRemoteController(grid);
                LoadFlightSeat(grid);
                LoadThrusters(grid);
                LoadGyros(grid);
                LoadBatteries(grid);
                LoadFuelTanks(grid);
                LoadLandingGear(grid);
                LoadConnector(grid);
                LoadRearDockArm(grid);
                LoadGangwayActuators(grid);
                LoadCamera(grid);
                AddAutopilotIndicatorSubscribers(grid);
                AddManeuverQueueSubscribers(grid);
                AddAmmoScreens(grid);
                LoadMedicalRoom(grid);
                LoadH2O2Generators(grid);
                LoadOxygenTanks(grid);
                LoadHydrogenEngines(grid);
                LoadGravityGenerators(grid);
                LoadMergeBlock(grid);
                LoadCargoContainers(grid);
                LoadGatlingGuns(grid);
                LoadAssaultCannonTurrets(grid);
                LoadRailguns(grid);
                LoadSensors(grid);
                LoadInteriorLights(grid);
                LoadButtonPanels(grid);
                LoadCryoPods(grid);
                LoadHangarMagPlates(grid);
                LoadDoors(grid);
                LoadAntenna(grid);
                LoadLowerDeckControlSeats(grid);
                LoadRefinery(grid);
                LoadAssembler(grid);
                LoadSpotlights(grid);
                LoadOreDetector(grid);

                AssignPowerClasses(grid);

                FlightController = new STUFlightController(grid, RemoteControl, me);

                DockingModule = new CBTDockingModule();
                ACM = new AirlockControlModule();
                ACM.LoadAirlocks(grid, me, runtime);

                AddToLogQueue("CBT initialized", STULogType.OK);
            }

            // high-level software interoperability methods and helpers
            #region High-Level Software Control Methods
            public static void EchoPassthru(string text)
            {
                echo(text);
            }

            // define the broadcaster method so that display messages can be sent throughout the world
            // (currently not implemented, just keeping this code here for future use)
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
                }
                    );

                AddToLogQueue($"just now finished Create Broadcast with message: {message}, key: {key}");
            }

            // define the method to send CBT log messages to the queue of all the screens on the CBT that are subscribed to such messages
            // actually pulling those messages from the queue and displaying them is done in UpdateLogScreens()
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
            #endregion

            // screen update methods
            #region Screen Update Methods
            // define the method to pull logs from the queue and display them on the screens
            // this will be called on every loop in Program.cs
            public static void UpdateLogScreens()
            {
                // get any logs generated by the flight controller and add them to the queue
                while (STUFlightController.FlightLogs.Count > 0)
                {
                    STULog log = STUFlightController.FlightLogs.Dequeue();
                    AddToLogQueue(log.Message, log.Type, log.Sender);
                }

                // update all the screens that are subscribed to the flight log, which each have their own queue of logs
                foreach (var screen in LogChannel)
                {
                    screen.StartFrame();
                    screen.WriteWrappableLogs(screen.FlightLogs);
                    screen.EndAndPaintFrame();
                }
            }

            public static void UpdateAutopilotScreens()
            {
                foreach (var screen in AutopilotStatusChannel)
                {
                    screen.StartFrame();
                    //if (GetAutopilotState() != 0) { 
                    //    screen.DrawAutopilotEnabledSprite(screen.CurrentFrame, screen.Center); 
                    //}
                    //else { 
                    //    screen.DrawAutopilotDisabledSprite(screen.CurrentFrame, screen.Center); 
                    //}
                    screen.DrawAutopilotStatus(screen.CurrentFrame, screen.Center);
                    screen.EndAndPaintFrame();
                }
            }

            public static void UpdateManeuverQueueScreens(ManeuverQueueData maneuverQueueData)
            {
                foreach (var screen in ManeuverQueueChannel)
                {
                    screen.StartFrame();
                    screen.LoadManeuverQueueData(maneuverQueueData);
                    screen.BuildManeuverQueueScreen(screen.CurrentFrame, screen.Center);
                    screen.EndAndPaintFrame();
                }
            }

            public static void UpdateAmmoScreens()
            {
                var inventory = InventoryEnumerator.GetItemTotals();
                foreach (var screen in AmmoChannel)
                {
                    screen.StartFrame();
                    screen.LoadAmmoData(
                        0, 0, 0 // fix and remove later
                                //inventory.ContainsKey("Gatling Ammo Box") ? (int)inventory["Gatling Ammo Box"] : 0,
                                //inventory.ContainsKey("Artillery Shell") ? (int)inventory["Artillery Shell"] : 0,
                                //inventory.ContainsKey("Large Railgun Sabot") ? (int)inventory["Large Railgun Sabot"] : 0
                        );
                    screen.BuildScreen(screen.CurrentFrame, screen.Center);
                    screen.EndAndPaintFrame();
                }
            }
            #endregion

            // initialize hardware on the CBT
            #region Hardware Initialization
            #region Screens
            // generate a list of the display blocks on the CBT that are subscribed to the flight log
            // do this by searching through all the blocks on the CBT and finding the ones whose custom data says they are subscribed
            private static void AddLogSubscribers(IMyGridTerminalSystem grid)
            {
                grid.GetBlocks(AllTerminalBlocks);
                foreach (var block in AllTerminalBlocks)
                {
                    string CustomDataRawText = block.CustomData;
                    string[] CustomDataLines = CustomDataRawText.Split('\n');
                    foreach (var line in CustomDataLines)
                    {
                        if (line.Contains("CBT_LOG"))
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
                                echo("caught exception in CBT.AddLogSubscribers():");
                                echo(e.Message);
                                fontSize = 0.5f;
                            }
                            CBTLogLCD screen = new CBTLogLCD(echo, block, int.Parse(kvp[1]), "Monospace", fontSize);
                            LogChannel.Add(screen);
                        }
                    }
                }
            }

            private static void AddAutopilotIndicatorSubscribers(IMyGridTerminalSystem grid)
            {
                grid.GetBlocks(AllTerminalBlocks);
                foreach (var block in AllTerminalBlocks)
                {
                    string CustomDataRawText = block.CustomData;
                    string[] CustomDataLines = CustomDataRawText.Split('\n');
                    foreach (var line in CustomDataLines)
                    {
                        if (line.Contains("CBT_AUTOPILOT_STATUS"))
                        {
                            string[] kvp = line.Split(':');
                            CBTAutopilotLCD screen = new CBTAutopilotLCD(echo, block, int.Parse(kvp[1]));
                            AutopilotStatusChannel.Add(screen);
                        }
                    }
                }
            }

            private static void AddManeuverQueueSubscribers(IMyGridTerminalSystem grid)
            {
                grid.GetBlocks(AllTerminalBlocks);
                foreach (var block in AllTerminalBlocks)
                {
                    string CustomDataRawText = block.CustomData;
                    string[] CustomDataLines = CustomDataRawText.Split('\n');
                    foreach (var line in CustomDataLines)
                    {
                        if (line.Contains("CBT_MANEUVER_QUEUE"))
                        {
                            string[] kvp = line.Split(':');
                            CBTManeuverQueueLCD screen = new CBTManeuverQueueLCD(echo, block, int.Parse(kvp[1]));
                            ManeuverQueueChannel.Add(screen);
                        }
                    }
                }
            }

            private static void AddAmmoScreens(IMyGridTerminalSystem grid)
            {
                grid.GetBlocks(AllTerminalBlocks);
                foreach (var block in AllTerminalBlocks)
                {
                    string CustomDataRawText = block.CustomData;
                    string[] CustomDataLines = CustomDataRawText.Split('\n');
                    foreach (var line in CustomDataLines)
                    {
                        if (line.Contains("CBT_AMMO"))
                        {
                            string[] kvp = line.Split(':');
                            CBTAmmoLCD screen = new CBTAmmoLCD(echo, block, int.Parse(kvp[1]));
                            AmmoChannel.Add(screen);
                        }
                    }
                }
            }
            #endregion

            #region Flight Critical
            private static void LoadRemoteController(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> remoteControlBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyRemoteControl>(remoteControlBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (remoteControlBlocks.Count == 0)
                {
                    AddToLogQueue("No remote control blocks found on the CBT", STULogType.ERROR);
                    return;
                }
                RemoteControl = remoteControlBlocks[0] as IMyRemoteControl;
                AddToLogQueue("Remote control ... loaded", STULogType.INFO);
            }
            // load main flight seat BY NAME. Name must be "CBT Flight Seat"
            private static void LoadFlightSeat(IMyGridTerminalSystem grid)
            {
                FlightSeat = grid.GetBlockWithName("CBT Flight Seat") as IMyTerminalBlock;
                if (FlightSeat == null)
                {
                    AddToLogQueue("Could not locate \"CBT Flight Seat\"; ensure flight seat is named appropriately", STULogType.ERROR);
                    return;
                }
                AddToLogQueue("Main flight seat ... loaded", STULogType.INFO);
            }
            // load ALL thrusters of ALL types
            // in later versions, fix this to have a list of ALL thrusters, plus subdivided groups of JUST ions and JUST hydros. 
            // even more generalized version of a ship's class should allow for atmo, but the CBT doesn't have atmo.
            private static void LoadThrusters(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> thrusterBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyThrust>(thrusterBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (thrusterBlocks.Count == 0)
                {
                    AddToLogQueue("No thrusters found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyThrust[] allThrusters = new IMyThrust[thrusterBlocks.Count];

                for (int i = 0; i < thrusterBlocks.Count; i++)
                {
                    allThrusters[i] = thrusterBlocks[i] as IMyThrust;
                }

                Thrusters = allThrusters;
                AddToLogQueue("Thrusters ... loaded", STULogType.INFO);
            }
            private static void LoadGyros(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> gyroBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGyro>(gyroBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gyroBlocks.Count == 0)
                {
                    AddToLogQueue("No gyros found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyGyro[] gyros = new IMyGyro[gyroBlocks.Count];
                for (int i = 0; i < gyroBlocks.Count; i++)
                {
                    gyros[i] = gyroBlocks[i] as IMyGyro;
                }

                Gyros = gyros;
                AddToLogQueue("Gyros ... loaded", STULogType.INFO);
            }
            private static void LoadBatteries(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> batteryBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (batteryBlocks.Count == 0)
                {
                    AddToLogQueue("No batteries found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyBatteryBlock[] batteries = new IMyBatteryBlock[batteryBlocks.Count];
                for (int i = 0; i < batteryBlocks.Count; i++)
                {
                    batteries[i] = batteryBlocks[i] as IMyBatteryBlock;
                }

                Batteries = batteries;
                AddToLogQueue("Batteries ... loaded", STULogType.INFO);
            }
            private static void LoadFuelTanks(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> gasTankBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGasTank>(gasTankBlocks, block => block.CubeGrid == Me.CubeGrid && block.BlockDefinition.SubtypeName.Contains("Hydrogen"));
                if (gasTankBlocks.Count == 0)
                {
                    AddToLogQueue("No fuel tanks found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyGasTank[] fuelTanks = new IMyGasTank[gasTankBlocks.Count];
                for (int i = 0; i < gasTankBlocks.Count; ++i)
                {
                    fuelTanks[i] = gasTankBlocks[i] as IMyGasTank;
                }

                HydrogenTanks = fuelTanks;
                AddToLogQueue("Fuel tanks ... loaded", STULogType.INFO);
            }
            #endregion Flight Critical

            // load landing gear
            private static void LoadLandingGear(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> landingGearBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyLandingGear>(landingGearBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (landingGearBlocks.Count == 0)
                {
                    AddToLogQueue("No landing gear found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyLandingGear[] landingGear = new IMyLandingGear[landingGearBlocks.Count];
                for (int i = 0; i < landingGearBlocks.Count; ++i)
                {
                    landingGear[i] = landingGearBlocks[i] as IMyLandingGear;
                }

                LandingGear = landingGear;
                AddToLogQueue("Landing gear ... loaded", STULogType.INFO);
            }

            public static void SetLandingGear(bool @lock)
            {
                foreach (var gear in LandingGear)
                {
                    if (@lock) gear.Lock();
                    else gear.Unlock();
                }
                LandingGearState = @lock;
            }

            public static void ToggleLandingGear()
            {
                foreach (var gear in LandingGear)
                {
                    if (LandingGearState) gear.Unlock();
                    else gear.Lock();
                }
            }

            // load connector (stinger)
            private static void LoadConnector(IMyGridTerminalSystem grid)
            {
                var connector = grid.GetBlockWithName("CBT Rear Connector");
                if (connector == null)
                {
                    AddToLogQueue("Could not locate \"CBT Rear Connector\"; ensure connector is named appropriately.", STULogType.ERROR);
                    return;
                }
                Connector = connector as IMyShipConnector;
                Connector.Enabled = true;
                Connector.IsParkingEnabled = false;
                Connector.PullStrength = 0;
                AddToLogQueue("Connector ... loaded", STULogType.INFO);
            }

            private static void LoadRearDockArm(IMyGridTerminalSystem grid)
            {
                var hinge1 = grid.GetBlockWithName("CBT Rear Hinge 1");
                var hinge2 = grid.GetBlockWithName("CBT Rear Hinge 2");
                var piston = grid.GetBlockWithName("CBT Rear Piston");
                if (hinge1 == null || hinge2 == null || piston == null)
                {
                    AddToLogQueue("Could not locate at least one stinger arm component; ensure all components are named appropriately", STULogType.ERROR);
                    return;
                }

                RearHinge1 = hinge1 as IMyMotorStator;
                RearHinge2 = hinge2 as IMyMotorStator;
                RearPiston = piston as IMyPistonBase;

                RearDock = new CBTRearDock(RearPiston, RearHinge1, RearHinge2, Connector);

                AddToLogQueue("Stinger arm actuator assembly ... loaded", STULogType.INFO);
            }

            private static void LoadGangwayActuators(IMyGridTerminalSystem grid)
            {
                var hinge1 = grid.GetBlockWithName("CBT Gangway Hinge 1");
                var hinge2 = grid.GetBlockWithName("CBT Gangway Hinge 2");
                if (hinge1 == null || hinge2 == null)
                {
                    AddToLogQueue("Could not locate at least one gangway actuator component; ensure all components are named appropriately", STULogType.ERROR);
                    return;
                }

                GangwayHinge1 = hinge1 as IMyMotorStator;
                GangwayHinge1.TargetVelocityRPM = 0;
                GangwayHinge2 = hinge2 as IMyMotorStator;
                GangwayHinge2.TargetVelocityRPM = 0;

                Gangway = new CBTGangway(GangwayHinge1, GangwayHinge2);

                AddToLogQueue("Gangway actuator assembly ... loaded", STULogType.INFO);
            }

            private static void LoadCamera(IMyGridTerminalSystem grid)
            {
                var rotor = grid.GetBlockWithName("CBT Camera Rotor");
                var hinge = grid.GetBlockWithName("CBT Camera Hinge");
                var camera = grid.GetBlockWithName("CBT Bottom Camera");
                if (rotor == null || hinge == null || camera == null)
                {
                    AddToLogQueue("Could not locate at least one camera component; ensure all components are named appropriately", STULogType.ERROR);
                    return;
                }

                CameraRotor = rotor as IMyMotorStator;
                CameraHinge = hinge as IMyMotorStator;
                Camera = camera as IMyCameraBlock;
                AddToLogQueue("Camera and actuator assembly ... loaded", STULogType.INFO);
            }

            #region Life Support
            private static void LoadMedicalRoom(IMyGridTerminalSystem grid)
            {
                MedicalRoom = grid.GetBlockWithName("CBT Medical Room") as IMyMedicalRoom;
                if (MedicalRoom == null)
                {
                    AddToLogQueue("Could not locate \"CBT Medical Room\"; ensure medical room is named appropriately", STULogType.ERROR);
                    return;
                }
                AddToLogQueue("Medical Room ... loaded", STULogType.INFO);
            }
            private static void LoadH2O2Generators(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> generatorBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGasGenerator>(generatorBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (generatorBlocks.Count == 0)
                {
                    AddToLogQueue("No H2O2 generators found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyGasGenerator[] generators = new IMyGasGenerator[generatorBlocks.Count];
                for (int i = 0; i < generatorBlocks.Count; i++)
                {
                    generators[i] = generatorBlocks[i] as IMyGasGenerator;
                }

                H2O2Generators = generators;
                AddToLogQueue("H2O2 generators ... loaded", STULogType.INFO);
            }
            private static void LoadOxygenTanks(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> gasTankBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGasTank>(gasTankBlocks, block => block.CubeGrid == Me.CubeGrid && block.BlockDefinition.SubtypeName.Contains("Oxygen"));
                if (gasTankBlocks.Count == 0)
                {
                    AddToLogQueue("No oxygen tanks found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyGasTank[] oxygenTanks = new IMyGasTank[gasTankBlocks.Count];
                for (int i = 0; i < gasTankBlocks.Count; ++i)
                {
                    oxygenTanks[i] = gasTankBlocks[i] as IMyGasTank;
                }

                OxygenTanks = oxygenTanks;
                AddToLogQueue("Oxygen tanks ... loaded", STULogType.INFO);
            }
            private static void LoadHydrogenEngines(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> hydrogenEngineBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyPowerProducer>(hydrogenEngineBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (hydrogenEngineBlocks.Count == 0)
                {
                    AddToLogQueue("No hydrogen engines found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyPowerProducer[] hydrogenEngines = new IMyPowerProducer[hydrogenEngineBlocks.Count];
                for (int i = 0; i < hydrogenEngineBlocks.Count; ++i)
                {
                    hydrogenEngines[i] = hydrogenEngineBlocks[i] as IMyPowerProducer;
                }

                HydrogenEngines = hydrogenEngines;
                AddToLogQueue("Hydrogen engines ... loaded", STULogType.INFO);
            }

            private static void LoadCryoPods(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> cryoPodBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyCryoChamber>(cryoPodBlocks, block => block.CubeGrid == Me.CubeGrid);

                IMyCryoChamber[] cryoPods = new IMyCryoChamber[cryoPodBlocks.Count];
                for (int i = 0; i < cryoPodBlocks.Count; i++)
                {
                    cryoPods[i] = cryoPodBlocks[i] as IMyCryoChamber;
                }

                AddToLogQueue("Cryo Pods ... loaded", STULogType.INFO);
            }
            #endregion Life Support

            // load gravity generators
            private static void LoadGravityGenerators(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> gravityGeneratorBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGravityGenerator>(gravityGeneratorBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gravityGeneratorBlocks.Count == 0)
                {
                    AddToLogQueue("No gravity generators found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyGravityGenerator[] gravityGenerators = new IMyGravityGenerator[gravityGeneratorBlocks.Count];
                for (int i = 0; i < gravityGeneratorBlocks.Count; ++i)
                {
                    gravityGenerators[i] = gravityGeneratorBlocks[i] as IMyGravityGenerator;
                }

                GravityGenerators = gravityGenerators;
                AddToLogQueue("Gravity generators ... loaded", STULogType.INFO);
            }
            private static void LoadSensors(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> sensorBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMySensorBlock>(sensorBlocks, block => block.CubeGrid == Me.CubeGrid);
                IMySensorBlock[] sensors = new IMySensorBlock[sensorBlocks.Count];
                for (int i = 0; i < sensorBlocks.Count; ++i)
                {
                    sensors[i] = sensorBlocks[i] as IMySensorBlock;
                }
                Sensors = sensors;
                AddToLogQueue("Sensors ... loaded", STULogType.INFO);
            }
            private static void LoadInteriorLights(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> lightBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyInteriorLight>(lightBlocks, block => block.CubeGrid == Me.CubeGrid);

                IMyInteriorLight[] interiorLights = new IMyInteriorLight[lightBlocks.Count];
                for (int i = 0; i < lightBlocks.Count; i++)
                {
                    interiorLights[i] = lightBlocks[i] as IMyInteriorLight;
                }

                AddToLogQueue("Interior Lights ... loaded", STULogType.INFO);
            }
            #region Weaponry
            private static void LoadGatlingGuns(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> gatlingGunBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyUserControllableGun>(gatlingGunBlocks, block => block.CubeGrid == Me.CubeGrid);

                List<IMyTerminalBlock> blocksToRemove = new List<IMyTerminalBlock>();
                foreach (var block in gatlingGunBlocks)
                {
                    if (block.BlockDefinition.SubtypeId != "LargeGatlingTurret")
                        blocksToRemove.Add(block);
                }
                foreach (var block in blocksToRemove) { gatlingGunBlocks.Remove(block); }
                if (gatlingGunBlocks.Count == 0) { AddToLogQueue("No gatling guns found on the CBT", STULogType.ERROR); return; }
                IMyUserControllableGun[] gatlingGuns = new IMyUserControllableGun[gatlingGunBlocks.Count];
                for (int i = 0; i < gatlingGunBlocks.Count; ++i)
                {
                    gatlingGuns[i] = gatlingGunBlocks[i] as IMyUserControllableGun;
                }

                GatlingTurrets = gatlingGuns;
                AddToLogQueue("Gatling guns ... loaded", STULogType.INFO);
            }
            private static void LoadAssaultCannonTurrets(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> assaultCannonBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyUserControllableGun>(assaultCannonBlocks, block => block.CubeGrid == Me.CubeGrid);

                List<IMyTerminalBlock> blocksToRemove = new List<IMyTerminalBlock>();
                foreach (var block in assaultCannonBlocks)
                {
                    if (block.BlockDefinition.SubtypeId != "LargeMissileTurret/LargeBlockMediumCalibreTurret")
                        blocksToRemove.Add(block);
                }
                foreach (var block in blocksToRemove) { assaultCannonBlocks.Remove(block); }
                if (assaultCannonBlocks.Count == 0) { AddToLogQueue("No assault cannons found on the CBT", STULogType.ERROR); return; }
                IMyUserControllableGun[] assaultCannons = new IMyUserControllableGun[assaultCannonBlocks.Count];
                for (int i = 0; i < assaultCannonBlocks.Count; ++i)
                {
                    assaultCannons[i] = assaultCannonBlocks[i] as IMyUserControllableGun;
                }

                AssaultCannons = assaultCannons;
                AddToLogQueue("Assault cannons ... loaded", STULogType.INFO);
            }
            private static void LoadRailguns(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> railgunBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyUserControllableGun>(railgunBlocks, block => block.CubeGrid == Me.CubeGrid);

                List<IMyTerminalBlock> blocksToRemove = new List<IMyTerminalBlock>();
                foreach (var block in railgunBlocks)
                {
                    if (block.BlockDefinition.SubtypeId != "LargeRailgunTurret")
                        blocksToRemove.Add(block);
                }
                foreach (var block in blocksToRemove) { railgunBlocks.Remove(block); }
                if (railgunBlocks.Count == 0) { AddToLogQueue("No railguns found on the CBT", STULogType.ERROR); return; }
                IMyUserControllableGun[] railguns = new IMyUserControllableGun[railgunBlocks.Count];
                for (int i = 0; i < railgunBlocks.Count; ++i)
                {
                    railguns[i] = railgunBlocks[i] as IMyUserControllableGun;
                }

                Railguns = railguns;
                AddToLogQueue("Railguns ... loaded", STULogType.INFO);
            }
            #endregion Weaponry

            #region Other

            private static void LoadMergeBlock(IMyGridTerminalSystem grid)
            {
                var mergeBlock = grid.GetBlockWithName("CBT Merge Block");
                if (mergeBlock == null)
                {
                    AddToLogQueue("Could not locate \"CBT Merge Block\"; ensure merge block is named appropriately", STULogType.ERROR);
                    return;
                }
                MergeBlock = mergeBlock as IMyShipMergeBlock;
                AddToLogQueue("Merge block ... loaded", STULogType.INFO);
            }

            private static void LoadButtonPanels(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> buttonPanelBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyButtonPanel>(buttonPanelBlocks, block => block.CubeGrid == Me.CubeGrid);
                IMyButtonPanel[] buttonPanels = new IMyButtonPanel[buttonPanelBlocks.Count];
                for (int i = 0; i < buttonPanelBlocks.Count; ++i)
                {
                    buttonPanels[i] = buttonPanelBlocks[i] as IMyButtonPanel;
                }
                ButtonPanels = buttonPanels;
                AddToLogQueue("Button panels ... loaded", STULogType.INFO);
            }

            private static void LoadHangarMagPlates(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> magPlateBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyShipConnector>(magPlateBlocks, block => block.CubeGrid == Me.CubeGrid);

                // for some reason, mag plates are considered connectors by space engineers
                // therefore, we need to remove any connectors we picked up in the GetBlocksOfType call above
                foreach (var item in magPlateBlocks)
                {
                    if (item.BlockDefinition.SubtypeName.Contains("Connector"))
                    {
                        magPlateBlocks.Remove(item);
                    }
                }

                IMyShipConnector[] magPlates = new IMyShipConnector[magPlateBlocks.Count];
                for (int i = 0; i < magPlateBlocks.Count; i++)
                {
                    magPlates[i] = magPlateBlocks[i] as IMyShipConnector;
                }
                HangarMagPlates = magPlates;
                AddToLogQueue("Hangar Mag Plates ... loaded", STULogType.INFO);
            }

            private static void LoadDoors(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> doorBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyDoor>(doorBlocks, block => block.CubeGrid == Me.CubeGrid);
                IMyDoor[] doors = new IMyDoor[doorBlocks.Count];
                for (int i = 0; i < doorBlocks.Count; i++)
                {
                    doors[i] = doorBlocks[i] as IMyDoor;
                }
                Doors = doors;
                AddToLogQueue("Doors ... loaded", STULogType.INFO);
            }

            private static void LoadAntenna(IMyGridTerminalSystem grid)
            {
                try
                {
                    Antenna = grid.GetBlockWithName("CBT Antenna") as IMyRadioAntenna;
                }
                catch
                {
                    AddToLogQueue("Error trying to find \"CBT Antenna\". Not loaded.", STULogType.WARNING);
                    return;
                }
                AddToLogQueue("Antenna ... loaded", STULogType.INFO);
            }

            private static void LoadLowerDeckControlSeats(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> controlSeatBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyCockpit>(controlSeatBlocks, block => block.CubeGrid == Me.CubeGrid);
                foreach (var block in controlSeatBlocks)
                {
                    if (block.BlockDefinition.SubtypeName.Contains("LargeBlockCockpit")) { }
                    else { controlSeatBlocks.Remove(block); }
                }
                IMyCockpit[] controlSeats = new IMyCockpit[controlSeatBlocks.Count];
                for (int i = 0; i < controlSeatBlocks.Count; i++)
                {
                    controlSeats[i] = controlSeatBlocks[i] as IMyCockpit;
                }
                LowerDeckControlSeats = controlSeats;
                AddToLogQueue("Lower deck control seats ... loaded", STULogType.INFO);
            }

            private static void LoadAssembler(IMyGridTerminalSystem grid)
            {
                try
                {
                    Assembler = grid.GetBlockWithName("CBT Assembler") as IMyAssembler;
                }
                catch
                {
                    AddToLogQueue("Error finding \"CBT Assembler\". Not loaded", STULogType.WARNING);
                    return;
                }
                AddToLogQueue("Assembler ... loaded", STULogType.INFO);
            }

            private static void LoadRefinery(IMyGridTerminalSystem grid)
            {
                try
                {
                    Refinery = grid.GetBlockWithName("CBT Refinery") as IMyRefinery;
                }
                catch
                {
                    AddToLogQueue("Error finding \"CBT Refinery\". Not loaded", STULogType.WARNING);
                    return;
                }
                AddToLogQueue("Refinery ... loaded", STULogType.INFO);
            }

            private static void LoadSpotlights(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> spotlightBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyReflectorLight>(spotlightBlocks, block => block.CubeGrid == Me.CubeGrid);
                IMyReflectorLight[] spotlights = new IMyReflectorLight[spotlightBlocks.Count];
                for (int i = 0; i < spotlightBlocks.Count; i++)
                {
                    spotlights[i] = spotlightBlocks[i] as IMyReflectorLight;
                }
                Spotlights = spotlights;
                AddToLogQueue("Spotlights ... loaded", STULogType.INFO);
            }

            private static void LoadOreDetector(IMyGridTerminalSystem grid)
            {
                try
                {
                    OreDetector = grid.GetBlockWithName("CBT Ore Detector") as IMyOreDetector;
                }
                catch
                {
                    AddToLogQueue("Error finding \"CBT Ore Detector\". Not loaded.", STULogType.WARNING);
                    return;
                }
                AddToLogQueue("Ore Detector ... loaded");
            }
            #endregion Other

            public static void AssignPowerClasses(IMyGridTerminalSystem grid)
            {
                /// level 1 power class:
                /// button panels are NOT functional blocks, omitted here
                /// Gatling guns
                List<IMyFunctionalBlock> level1blocks = new List<IMyFunctionalBlock>();
                foreach (var item in GatlingTurrets)
                {
                    level1blocks.Add(item);
                }
                PowerLevelClassificationTable[1] = level1blocks;

                /// level 2 power class:
                /// O2 tanks
                /// Cryo pods are NOT functional blocks, omitted here
                /// H2 tanks
                /// Hangar mag plates
                /// connectors (the CBT only has the one connector on the stinger, and due to inherited LIGMA code, it must be named "Connector"... I think.
                List<IMyFunctionalBlock> level2blocks = new List<IMyFunctionalBlock>();
                foreach (var item in OxygenTanks)
                {
                    level2blocks.Add(item);
                }
                foreach (var item in HydrogenTanks)
                {
                    level2blocks.Add(item);
                }
                foreach (var item in HangarMagPlates)
                {
                    level2blocks.Add(item);
                }
                level2blocks.Add(Connector);
                PowerLevelClassificationTable[2] = level2blocks;

                /// level 3 power class:
                /// doors
                /// lights, interior and exterior
                /// gravity generator
                /// med bay
                /// hinges, pistons, rotors
                /// cameras
                /// LCD panels
                List<IMyFunctionalBlock> level3blocks = new List<IMyFunctionalBlock>();
                foreach (var item in Doors)
                {
                    level3blocks.Add(item);
                }
                foreach (var item in InteriorLights)
                {
                    level3blocks.Add(item);
                }
                foreach (var item in Spotlights)
                {
                    level3blocks.Add(item);
                }
                foreach (var item in GravityGenerators)
                {
                    level3blocks.Add(item);
                }
                level3blocks.Add(MedicalRoom);
                List<IMyTerminalBlock> allHingesAndRotors = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyMotorStator>(allHingesAndRotors, block => block.CubeGrid == Me.CubeGrid);
                foreach (var item in allHingesAndRotors)
                {
                    level3blocks.Add(item as IMyFunctionalBlock); // this chicanery might break...
                }
                List<IMyTerminalBlock> allPistons = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyPistonBase>(allPistons, block => block.CubeGrid == Me.CubeGrid);
                foreach (var item in allPistons)
                {
                    level3blocks.Add(item as IMyFunctionalBlock);
                }
                level3blocks.Add(Camera);
                List<IMyTerminalBlock> allLCDs = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyTextPanel>(allLCDs, block => block.CubeGrid == Me.CubeGrid);
                foreach (var item in allLCDs)
                {
                    level3blocks.Add(item as IMyFunctionalBlock);
                }
                PowerLevelClassificationTable[3] = level3blocks;

                /// level 4 power class:
                /// Antenna
                /// Air vents
                /// h2/o2 generators
                List<IMyFunctionalBlock> level4blocks = new List<IMyFunctionalBlock>();
                level4blocks.Add(Antenna);
                List<IMyTerminalBlock> airVents = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyAirVent>(airVents, block => block.CubeGrid == Me.CubeGrid);
                foreach (var item in airVents)
                {
                    level4blocks.Add(item as IMyFunctionalBlock);
                }
                foreach (var item in H2O2Generators)
                {
                    level4blocks.Add(item);
                }
                PowerLevelClassificationTable[4] = level4blocks;

                /// level 5 power class:
                /// Assault Cannons
                /// Lower deck control seats
                /// railguns
                List<IMyFunctionalBlock> level5blocks = new List<IMyFunctionalBlock>();
                foreach (var item in AssaultCannons)
                {
                    level5blocks.Add(item);
                }
                foreach (var item in LowerDeckControlSeats)
                {
                    level5blocks.Add(item as IMyFunctionalBlock);
                }
                foreach (var item in Railguns)
                {
                    level5blocks.Add(item);
                }
                PowerLevelClassificationTable[5] = level5blocks;

                /// level 6 power class:
                /// Assembler
                /// Refinery
                List<IMyFunctionalBlock> level6blocks = new List<IMyFunctionalBlock>();
                level6blocks.Add(Assembler);
                level6blocks.Add(Refinery);
                PowerLevelClassificationTable[6] = level6blocks;

                /// level 7 power class:
                /// sensors
                /// ore detector
                List<IMyFunctionalBlock> level7blocks = new List<IMyFunctionalBlock>();
                foreach (var item in Sensors)
                {
                    level7blocks.Add(item);
                }
                level7blocks.Add(OreDetector);
                PowerLevelClassificationTable[7] = level7blocks;

            }
            #endregion Hardware Initialization

            // inventory management methods
            #region Inventory Management
            private static void LoadCargoContainers(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> cargoContainerBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyCargoContainer>(cargoContainerBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (cargoContainerBlocks.Count == 0)
                {
                    AddToLogQueue("No cargo containers found on the CBT", STULogType.ERROR);
                    return;
                }

                IMyCargoContainer[] cargoContainers = new IMyCargoContainer[cargoContainerBlocks.Count];
                for (int i = 0; i < cargoContainerBlocks.Count; ++i)
                {
                    cargoContainers[i] = cargoContainerBlocks[i] as IMyCargoContainer;
                }

                CargoContainers = cargoContainers;
                AddToLogQueue("Cargo containers ... loaded", STULogType.INFO);
            }

            public static int GetAmmoLevel(string ammoType)
            {
                int ammo = 0;

                // seach cargo containers
                foreach (var container in CargoContainers)
                {
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    container.GetInventory(0).GetItems(items);
                    foreach (var item in items)
                    {
                        if (item.Type.SubtypeId == ammoType)
                        {
                            ammo += (int)item.Amount;
                        }
                    }
                }

                // search guns themselves
                return ammo;
            }

            #endregion

            // CBT helper functions
            #region CBT Helper Functions
            public static int GetAutopilotState()
            {
                int autopilotState = 0;
                if (FlightController.HasThrusterControl) { autopilotState += 1; }
                if (FlightController.HasGyroControl) { autopilotState += 2; }
                if (!RemoteControl.DampenersOverride) { autopilotState += 4; }
                // 0 = no autopilot
                // 1 = thrusters only
                // 2 = gyros only
                // 3 = thrusters and gyros
                // 4 = dampeners only
                // 5 = thrusters and dampeners
                // 6 = gyros and dampeners
                // 7 = all three
                return autopilotState;
            }

            public static void SetAutopilotControl(bool thrusters, bool gyroscopes, bool dampeners)
            {
                if (thrusters) { FlightController.ReinstateThrusterControl(); } else { FlightController.RelinquishThrusterControl(); }
                if (gyroscopes) { FlightController.ReinstateGyroControl(); } else { FlightController.RelinquishGyroControl(); }
                RemoteControl.DampenersOverride = dampeners;
            }

            public static void SetAutopilotControl(int state)
            {
                bool thrusters = false;
                bool gyros = false;
                bool dampeners = true;
                switch (state)
                {
                    case 1:
                        thrusters = true;
                        break;
                    case 2:
                        gyros = true;
                        break;
                    case 3:
                        thrusters = true;
                        gyros = true;
                        break;
                    case 4:
                        dampeners = false;
                        break;
                    case 5:
                        thrusters = true;
                        dampeners = false;
                        break;
                    case 6:
                        gyros = true;
                        dampeners = false;
                        break;
                    case 7:
                        thrusters = true;
                        gyros = true;
                        dampeners = false;
                        break;
                }
                SetAutopilotControl(thrusters, gyros, dampeners);
            }

            public static void SetPowerLevel(int powerLevel)
            {
                for (int i = 0; i < PowerLevelClassificationTable.Count; i++)
                {
                    foreach (var item in PowerLevelClassificationTable[i])
                    {
                        if (i <= powerLevel) { item.Enabled = true; }
                        else { item.Enabled = false; }
                    }
                }

            }

            public static void ResetUserInputVelocities()
            {
                UserInputForwardVelocity = 0;
                UserInputRightVelocity = 0;
                UserInputUpVelocity = 0;
                UserInputRollVelocity = 0;
                UserInputPitchVelocity = 0;
                UserInputYawVelocity = 0;
            }

            public static void SetCruisingAltitude(double altitude)
            {
                SetAutopilotControl(true, false, false);
                FlightController.MaintainSeaLevelAltitude(altitude, 5, 5);
            }
            #endregion
        }
    }
}
