using Sandbox.ModAPI.Ingame;
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
            public static Vector3D LaunchCoordinates { get; set; }

            public const float TimeStep = 1.0f / 6.0f;
            public static float Timestamp = 0;

            /// <summary>
            ///  pull in blocks from the grid
            /// </summary>
            public static STUFlightController FlightController { get; set; }
            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static IMyShipConnector Connector { get; set; } // fix this later, Ethan said something about the LIGMA code assuming exactly one connector
            public static IMyRemoteControl RemoteControl { get; set; }
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

            // instantiate the CBT object for the CBT model in game
            public CBT(STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime)
            {
                Me = me;
                Broadcaster = broadcaster;
                Runtime = runtime;

                FlightController = new STUFlightController(RemoteControl, TimeStep, Thrusters, Gyros);
            }

            // instantiate the broadcaster so that display messages can be sent throughout the ship and world
            public static void CreateBroadcast(string message, string type)
            {
                Broadcaster.Log(new STULog
                {
                    Sender = CBT_VARIABLES.CBT_VEHICLE_NAME,
                    Message = message,
                    Type = type,
                });
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


        }
    }
}
