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

            public static Vector3D NextWaypoint;

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
            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static IMyShipConnector Connector { get; set; } // fix this later, Ethan said something about the LIGMA code assuming exactly one connector
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
                Executing
            }

            public enum PowerStates
            {
                Normal,
                Low
            }

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
                LoadConnector(grid);
                AddAutopilotIndicatorSubscribers(grid);
                LoadMedicalRoom(grid);
                LoadH2O2Generators(grid);
                LoadOxygenTanks(grid);
                LoadHydrogenEngines(grid);
                LoadGravityGenerators(grid);

                FlightSeat = grid.GetBlockWithName("CBT Flight Seat") as IMyTerminalBlock;

                FlightController = new STUFlightController(RemoteControl, Thrusters, Gyros);

                AddToLogQueue("CBT initialized", STULogType.OK);
                UpdateLogScreens();
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

            public static void UpdateAutopilotScreens(bool status)
            {
                foreach (var screen in AutopilotStatusChannel)
                {
                    screen.StartFrame();
                    if (status) { 
                        screen.SetupDrawSurface(screen.Surface, status); 
                        screen.DrawAutopilotEnabledSprite(screen.CurrentFrame, screen.Center); 
                    }
                    else { 
                        screen.SetupDrawSurface(screen.Surface, status); 
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
                AddToLogQueue("Remote control ... loaded", STULogType.INFO);
            }

            // load main flight seat BY NAME. Name must be "CBT Flight Seat"
            private static void LoadFlightSeat(IMyGridTerminalSystem grid)
            {
                FlightSeat = grid.GetBlockWithName("CBT Flight Seat") as IMyControlPanel;
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

            // load connector (stinger)
            private static void LoadConnector(IMyGridTerminalSystem grid)
            {
                var connector = grid.GetBlockWithName("CBT Rear Connector");
                if (connector == null)
                {
                    AddToLogQueue("Could not locate \"CBT Rear Connector\"; ensure connector is named appropriately. Also only one allowed", STULogType.ERROR);
                    return;
                }
                Connector = connector as IMyShipConnector;
                AddToLogQueue("Connector ... loaded", STULogType.INFO);
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
                AddToLogQueue("Gravity generators ... loaded", STULogType.INFO);
            }

            
            /// <summary>
            /// flight modes and power management modes will be defined here for now until I figure out how to generalize them for future, generic aircraft
            /// All flight modes must behave as a state machine, returning a boolean value to indicate whether the mode is complete or not.
            /// </summary>
            /// <returns></returns>
            /// 

            // power modes
            //public static void PowerMode(Enum state)
            //{
            //    switch (state)
            //    {
            //        case PowerStates.Normal:
            //            // check power levels and adjust accordingly
            //            break;
            //        case PowerStates.Low:
            //            // check power levels and adjust accordingly
            //            break;
            //    }
            //}

            // "Hover" mode
            public static bool Hover()
            {
                FlightController.ReinstateGyroControl();
                FlightController.ReinstateThrusterControl();
                UserInputForwardVelocity = 0;
                UserInputRightVelocity = 0;
                UserInputUpVelocity = 0;
                UserInputRollVelocity = 0;
                UserInputPitchVelocity = 0;
                UserInputYawVelocity = 0;
                
                bool VxStable = FlightController.SetVx(0);
                bool VzStable = FlightController.SetVz(0);
                bool VyStable = FlightController.SetVy(0);
                FlightController.SetVr(0);
                FlightController.SetVp(0);
                FlightController.SetVw(0);
                return VxStable && VzStable && VyStable;
            }

            public static bool CruisingSpeed()
            {
                FlightController.RelinquishGyroControl();
                FlightController.ReinstateThrusterControl();
                bool stable = FlightController.SetVz(UserInputForwardVelocity);
                bool VxStable = FlightController.SetVx(0);
                bool VyStable = FlightController.SetVy(0);
                return stable;
            }

            public static void CruisingAltitude(double altitude)
            {
                FlightController.RelinquishGyroControl();
                FlightController.ReinstateThrusterControl();
                FlightController.MaintainAltitude(altitude);
            }

            public static bool FastStop()
            {
                // get current velocity
                Vector3D currentVelocity = RemoteControl.GetShipVelocities().LinearVelocity;

                return true;
            }

            public static bool GenericManeuver()
            {
                FlightController.ReinstateGyroControl();
                FlightController.ReinstateThrusterControl();
                bool VzStable = FlightController.SetVz(UserInputForwardVelocity); 
                bool VxStable = FlightController.SetVx(UserInputRightVelocity);
                bool VyStable = FlightController.SetVy(UserInputUpVelocity);
                FlightController.SetVr(UserInputRollVelocity * -1); // roll is inverted for some reason and is the only one that works like this on the CBT, not sure about other ships
                FlightController.SetVp(UserInputPitchVelocity);
                FlightController.SetVw(UserInputYawVelocity);
                return VxStable && VzStable && VyStable;
            }

            public static bool PointAtTarget()
            {
                AddToLogQueue("Pointing at target", STULogType.INFO);
                FlightController.ReinstateGyroControl();
                FlightController.ReinstateThrusterControl();
                return FlightController.AlignShipToTarget(NextWaypoint);
            }

            // "AC130" mode
            // radius is the radius from the center point that the CBT should travel in its circle
            // the next three arguments are the coordinates of the center of the proposed circle
            // speed is the time it should take the CBT to complete one revolution of the circle, in seconds.
            public static void AC130(double radius, double xCoord, double yCoord, double zCoord, double seconds)
            {
                // I need to write a lot of code to figure out a flight plan for an arbitrary AC130 flight pattern
            }
        }
    }
}
