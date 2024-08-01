using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBT
        {

            public static string LastErrorMessage = "";

            public const float TimeStep = 1.0f / 6.0f;
            public static float Timestamp = 0;
            public static Phase CurrentPhase = Phase.Idle;

            public static float UserInputForwardVelocity = 0;
            public static float UserInputRightVelocity = 0;
            public static float UserInputUpVelocity = 0;
            public static float UserInputRollVelocity = 0;
            public static float UserInputPitchVelocity = 0;
            public static float UserInputYawVelocity = 0;

            /// <summary>
            ///  prepare the program by declaring all the different blocks we are going to use
            /// </summary>
            // this may be potentially confusing, but "GridTerminalSystem" as it is commonly used in Program.cs to get blocks from the grid
            // does not exist in this namespace. Therefore, we are creating a new GridTerminalSystem object here to use in this class.
            // I could have named it whatever, e.g. "CBTGrid" but I don't want to have too many different names for the same thing.
            // just understand that when I reference the GridTerminalSystem property of the CBT class, I am referring to this object and NOT the one in Program.cs
            public static IMyGridTerminalSystem CBTGrid;
            public static List<IMyTerminalBlock> AllTerminalBlocks = new List<IMyTerminalBlock>();
            public static List<LogLCDs> LogChannel = new List<LogLCDs>();
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
            public static IMyGasTank[] GasTanks { get; set; }

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

            // define the CBT object for the CBT model in game
            public CBT(STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime)
            {
                Me = me;
                Broadcaster = broadcaster;
                Runtime = runtime;
                CBTGrid = grid;

                LoadRemoteController(grid);
                LoadFlightSeat(grid);
                LoadThrusters(grid);
                LoadGyros(grid);
                LoadBatteries(grid);
                LoadFuelTanks(grid);
                LoadConnector(grid);
                AddSubscribers(grid);

                FlightSeat = grid.GetBlockWithName("CBT Flight Seat") as IMyTerminalBlock;

                FlightSeatFarLeftScreen = new STUDisplay(FlightSeat, 3);
                FlightSeatLeftScreen = new STUDisplay(FlightSeat, 1);
                PBMainScreen = new STUDisplay(Me, 0);

                FlightController = new STUFlightController(RemoteControl, Thrusters, Gyros);

                AddToLogQueue("CBT initialized", STULogType.OK);
                AddToLogQueue("CBT initialized for sure", STULogType.OK);
                UpdateLogScreens();
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
            public static void AddToLogQueue(string message, string type)
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
                    screen.UpdateDisplay();
                }
            }

            // generate a list of the display blocks on the CBT that are subscribed to the flight log
            // do this by searching through all the blocks on the CBT and finding the ones whose custom data says they are subscribed
            private static void AddSubscribers(IMyGridTerminalSystem grid)
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
                            LogLCDs screen = new LogLCDs(block, int.Parse(kvp[1]), "Monospace", 0.5f);
                            screen.DefaultLineHeight += 5;
                            LogChannel.Add(screen);
                        }
                    }
                }
            }

            // initialize hardware on the CBT

            // load remote controller
            private static void LoadRemoteController(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> remoteControlBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyRemoteControl>(remoteControlBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (remoteControlBlocks.Count == 0)
                {
                    CreateBroadcast("No remote control blocks found on the CBT", STULogType.ERROR);
                }
                RemoteControl = remoteControlBlocks[0] as IMyRemoteControl;
                CreateBroadcast("Remote control ... loaded", STULogType.OK);
            }

            // load main flight seat BY NAME. Name must be "CBT Flight Seat"
            private static void LoadFlightSeat(IMyGridTerminalSystem grid)
            {
                FlightSeat = grid.GetBlockWithName("CBT Flight Seat") as IMyControlPanel;
                CreateBroadcast("Main flight seat ... loaded", STULogType.OK);
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
                    CreateBroadcast("No thrusters found on the CBT", STULogType.ERROR);
                }

                IMyThrust[] allThrusters = new IMyThrust[thrusterBlocks.Count];

                for (int i = 0; i < thrusterBlocks.Count; i++)
                {
                    allThrusters[i] = thrusterBlocks[i] as IMyThrust;
                }

                Thrusters = allThrusters;
                CreateBroadcast("Thrusters ... loaded", STULogType.OK);
            }

            // load gyroscopes
            private static void LoadGyros(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> gyroBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGyro>(gyroBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gyroBlocks.Count == 0)
                {
                    CreateBroadcast("No gyros found on the CBT", STULogType.ERROR);
                }

                IMyGyro[] gyros = new IMyGyro[gyroBlocks.Count];
                for (int i = 0; i < gyroBlocks.Count; i++)
                {
                    gyros[i] = gyroBlocks[i] as IMyGyro;
                }

                Gyros = gyros;
                CreateBroadcast("Gyros ... loaded", STULogType.OK);
            }

            // load batteries
            private static void LoadBatteries(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> batteryBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (batteryBlocks.Count == 0)
                {
                    CreateBroadcast("No batteries found on the CBT", STULogType.ERROR);
                }

                IMyBatteryBlock[] batteries = new IMyBatteryBlock[batteryBlocks.Count];
                for (int i = 0; i < batteryBlocks.Count; i++)
                {
                    batteries[i] = batteryBlocks[i] as IMyBatteryBlock;
                }

                Batteries = batteries;
                CreateBroadcast("Batteries ... loaded", STULogType.OK);
            }

            // load fuel tanks
            private static void LoadFuelTanks(IMyGridTerminalSystem grid)
            {
                List<IMyTerminalBlock> gasTankBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGasTank>(gasTankBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gasTankBlocks.Count == 0)
                {
                    CreateBroadcast("No fuel tanks found on the CBT", STULogType.ERROR);
                }

                IMyGasTank[] fuelTanks = new IMyGasTank[gasTankBlocks.Count];
                for (int i = 0; i < gasTankBlocks.Count; ++i)
                {
                    fuelTanks[i] = gasTankBlocks[i] as IMyGasTank;
                }

                GasTanks = fuelTanks;
                CreateBroadcast("Fuel tanks ... loaded", STULogType.OK);
            }

            // load connector (stinger)
            private static void LoadConnector(IMyGridTerminalSystem grid)
            {
                var connector = grid.GetBlockWithName("CBT Rear Connector");
                if (connector == null)
                {
                    CreateBroadcast("Could not locate \"CBT Rear Connector\"; ensure connector is named appropriately. Also only one allowed", STULogType.ERROR);
                }
                Connector = connector as IMyShipConnector;
                CreateBroadcast("Connector ... loaded", STULogType.OK);
            }

            /// <summary>
            /// flight modes will be defined here for now until I figure out how to generalize them for future, generic aircraft
            /// All flight modes must behave as a state machine, returning a boolean value to indicate whether the mode is complete or not.
            /// </summary>
            /// <returns></returns>

            // "Hover" mode
            public static bool Hover()
            {
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

            public static bool GenericManeuver()
            {
                bool VzStable = FlightController.SetVz(UserInputForwardVelocity); 
                bool VxStable = FlightController.SetVx(UserInputRightVelocity);
                bool VyStable = FlightController.SetVy(UserInputUpVelocity);
                FlightController.SetVr(UserInputRollVelocity);
                FlightController.SetVp(UserInputPitchVelocity);
                FlightController.SetVw(UserInputYawVelocity);
                return VxStable && VzStable && VyStable;
            }

            // "AC130" mode
            // radius is the radius from the center point that the CBT should travel in its circle
            // the next three arguments are the coordinates of the center of the proposed circle
            // speed is the time it should take the CBT to complete one revolution of the circle, in seconds.
            public static void AC130(double radius, double xCoord, double yCoord, double zCoord, double seconds)
            {
                // I need to write a lot of code to figure out a flight plan for an arbitrary AC130 flight pattern
                FlightController.SetVy(5);
                // FlightController.SetVw(5); this would be set yaw velocity, which needs to be built as it was not necessary for LIGMA.
            }
        }
    }
}
