﻿using Sandbox.Game.Screens.DebugScreens;
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
            public static CBTRearDock.RearDockStates UserInputRearDockState;
            public static string UserInputRearDockPort;

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
            public static IMyCargoContainer[] CargoContainers { get; set; }

            public static IMyMedicalRoom MedicalRoom { get; set; }
            public static IMyGasGenerator[] H2O2Generators { get; set; }
            public static IMyPowerProducer[] HydrogenEngines { get; set; }
            public static IMyGravityGenerator[] GravityGenerators { get; set; }
            public static IMySensorBlock[] Sensors { get; set; }
            public static IMyInteriorLight[] InteriorLights { get; set; }
            public static IMyUserControllableGun[] GatlingTurrets { get; set; }
            public static IMyUserControllableGun[] AssaultCannons { get; set; }
            public static IMyUserControllableGun[] Railguns { get; set; }

            /// <summary>
            /// establish fuel and power levels
            /// 
            public static double CurrentFuel { get; set; }
            public static double CurrentPower { get; set; }
            public static double FuelCapacity { get; set; }
            public static double PowerCapacity { get; set; }

            public static bool LandingGearState { get; set; }

            
            // define generic phases for executing flight plans, essentially a state machine
            public enum Phase
            {
                Idle,
                Executing,
            }

            public enum PowerStates
            {
                Normal,
                Low
            }

            public enum HardwareClassificationLevels
            {
                FlightCritical,
                LifeSupport,
                Radio,
                Production,
                Weaponry,
                Other,
            }

            public static Dictionary<HardwareClassificationLevels, List<IMyTerminalBlock>> PowerStateLookupTable = new Dictionary<HardwareClassificationLevels, List<IMyTerminalBlock>>()
            {
                { HardwareClassificationLevels.FlightCritical, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.LifeSupport, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.Radio, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.Production, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.Weaponry, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.Other, new List<IMyTerminalBlock> { } },
            };

            // CBT object instantiation
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
                LoadCargoContainers(grid);
                LoadGatlingGuns(grid);
                LoadAssaultCannonTurrets(grid);
                LoadRailguns(grid);

                FlightController = new STUFlightController(grid, RemoteControl, me);

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
                    if (GetAutopilotState() != 0) { 
                        screen.DrawAutopilotEnabledSprite(screen.CurrentFrame, screen.Center); 
                    }
                    else { 
                        screen.DrawAutopilotDisabledSprite(screen.CurrentFrame, screen.Center); 
                    }
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
                        inventory.ContainsKey("Gatling Ammo Box") ? (int)inventory["Gatling Ammo Box"] : 0,
                        inventory.ContainsKey("Artillery Shell") ? (int)inventory["Artillery Shell"] : 0,
                        inventory.ContainsKey("Large Railgun Sabot") ? (int)inventory["Large Railgun Sabot"] : 0
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
                            catch(Exception e)
                            {
                                echo("caught exception in AddLogSubscribers:");
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.FlightCritical, out existingList);
                existingList.Add(RemoteControl);
                PowerStateLookupTable[HardwareClassificationLevels.FlightCritical] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.FlightCritical, out existingList);
                existingList.AddRange(Thrusters);
                PowerStateLookupTable[HardwareClassificationLevels.FlightCritical] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.FlightCritical, out existingList);
                existingList.AddRange(Gyros);
                PowerStateLookupTable[HardwareClassificationLevels.FlightCritical] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.FlightCritical, out existingList);
                existingList.AddRange(Batteries);
                PowerStateLookupTable[HardwareClassificationLevels.FlightCritical] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.LifeSupport, out existingList);
                existingList.Add(MedicalRoom);
                PowerStateLookupTable[HardwareClassificationLevels.LifeSupport] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.Production, out existingList);
                existingList.AddRange(H2O2Generators);
                PowerStateLookupTable[HardwareClassificationLevels.Production] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.LifeSupport, out existingList);
                existingList.AddRange(OxygenTanks);
                PowerStateLookupTable[HardwareClassificationLevels.LifeSupport] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.Other, out existingList);
                existingList.AddRange(GravityGenerators);
                PowerStateLookupTable[HardwareClassificationLevels.Other] = existingList;
                AddToLogQueue("Gravity generators ... loaded", STULogType.INFO);
            }
            private static void LoadSensors(IMyGridTerminalSystem grid)
            {
                // load sensors
            }
            private static void LoadInteriorLights(IMyGridTerminalSystem grid)
            {
                // load interior lights
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.Weaponry, out existingList);
                existingList.AddRange(GatlingTurrets);
                PowerStateLookupTable[HardwareClassificationLevels.Weaponry] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.Weaponry, out existingList);
                existingList.AddRange(AssaultCannons);
                PowerStateLookupTable[HardwareClassificationLevels.Weaponry] = existingList;
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
                List<IMyTerminalBlock> existingList;
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.Weaponry, out existingList);
                existingList.AddRange(Railguns);
                PowerStateLookupTable[HardwareClassificationLevels.Weaponry] = existingList;
                AddToLogQueue("Railguns ... loaded", STULogType.INFO);
            }
            #endregion Weaponry
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

            public static void PowerModeControl(HardwareClassificationLevels inputHardwareList, bool desiredState)
            {
                List<IMyTerminalBlock> retrievedHardwareList;
                if (PowerStateLookupTable.TryGetValue(inputHardwareList, out retrievedHardwareList))
                {
                    foreach (var hardware in retrievedHardwareList)
                    {
                        if (hardware is IMyFunctionalBlock)
                        {
                            IMyFunctionalBlock functionalBlock = hardware as IMyFunctionalBlock;
                            functionalBlock.Enabled = desiredState;
                        }
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
                FlightController.MaintainSeaLevelAltitude(altitude);
            }
            #endregion
        }
    }
}
