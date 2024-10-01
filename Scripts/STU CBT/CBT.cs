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
            public static STUFlightController FlightController { get; set; }
            public static CBTGangway Gangway { get; set; }
            public static CBTRearDock RearDock { get; set; }
            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
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

            public static IMyMedicalRoom MedicalRoom { get; set; }
            public static IMyGasGenerator[] H2O2Generators { get; set; }
            public static IMyPowerProducer[] HydrogenEngines { get; set; }
            public static IMyGravityGenerator[] GravityGenerators { get; set; }

            /// <summary>
            /// establish fuel and power levels
            /// 
            public static double CurrentFuel { get; set; }
            public static double CurrentPower { get; set; }
            public static double FuelCapacity { get; set; }
            public static double PowerCapacity { get; set; }
            public static bool LandingGearState { get; set; }

            // enumerate flight modes for the CBT, similar to how the LIGMA has flight plans / phases
            public enum Mode
            {
                AC130,
                Hover,
            }

            // define generic phases for executing flight plans, essentially a state machine
            public enum Phase
            {
                Idle,
                Executing,
                GracefulExit,
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
                Other,
            }

            public static Dictionary<HardwareClassificationLevels, List<IMyTerminalBlock>> PowerStateLookupTable = new Dictionary<HardwareClassificationLevels, List<IMyTerminalBlock>>()
            {
                { HardwareClassificationLevels.FlightCritical, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.LifeSupport, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.Radio, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.Production, new List<IMyTerminalBlock> { } },
                { HardwareClassificationLevels.Other, new List<IMyTerminalBlock> { } },
            };

            // define the CBT object for the CBT model in game
            public CBT(Action<string> Echo, STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime)
            {
                Me = me;
                Broadcaster = broadcaster;
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
                LoadMedicalRoom(grid);
                LoadH2O2Generators(grid);
                LoadOxygenTanks(grid);
                LoadHydrogenEngines(grid);
                LoadGravityGenerators(grid);

                FlightController = new STUFlightController(grid, RemoteControl, me);

                AddToLogQueue("CBT initialized", STULogType.OK);
            }

            public static void EchoPassthru(string text)
            {
                echo(text);
            }

            // define the broadcaster method so that display messages can be sent throughout the world
            // (currently not implemented, just keeping this code here for future use)
            public static void CreateBroadcast(string message, string type)
            {
                Broadcaster.Log(new STULog
                {
                    Sender = CBT_VARIABLES.CBT_VEHICLE_NAME,
                    Message = message,
                    Type = type,
                });
            }

            // define the method to send CBT log messages to the queue of all the screens on the CBT that are subscribed to such messages
            // actually pulling those messages from the queue and displaying them is done in UpdateLogScreens()
            public static void AddToLogQueue(string message, string type = STULogType.INFO)
            {
                foreach (var screen in LogChannel)
                {
                    screen.FlightLogs.Enqueue(new STULog
                    {
                        Sender = CBT_VARIABLES.CBT_VEHICLE_NAME,
                        Message = message,
                        Type = type,
                    });
                }
            }

            // define the method to pull logs from the queue and display them on the screens
            // this will be called on every loop in Program.cs
            public static void UpdateLogScreens()
            {
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

            /// initialize hardware on the CBT


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

            // load remote controller
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
                PowerStateLookupTable.TryGetValue(HardwareClassificationLevels.Radio, out existingList);
                existingList.Add(RemoteControl);
                PowerStateLookupTable[HardwareClassificationLevels.Radio] = existingList;
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

            // load gyroscopes
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

            // load batteries
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

            // load fuel tanks
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

            // load med bay
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

            // load H2O2 generators
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

            // load oxygen tanks
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

            // load hydrogen engines
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

            
            /// <summary>
            /// flight modes and power management modes will be defined here for now until I figure out how to generalize them for future, generic aircraft
            /// All flight modes must behave as a state machine, returning a boolean value to indicate whether the mode is complete or not.
            /// </summary>
            /// <returns></returns>
            /// 

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

            public static bool SetCruisingSpeed()
            {
                SetAutopilotControl(true, false, false);
                bool stable = FlightController.SetVz(UserInputForwardVelocity);
                bool VxStable = FlightController.SetVx(0);
                bool VyStable = FlightController.SetVy(0);
                return stable && VxStable && VyStable;
            }

            public static void SetCruisingAltitude(double altitude)
            {
                SetAutopilotControl(true, false, false);
                FlightController.MaintainSeaLevelAltitude(altitude);
            }
        }
    }
}
