using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public struct Planet {
                public double Radius;
                public Vector3D Center;
            }

            public static Dictionary<string, Planet> CelestialBodies = new Dictionary<string, Planet> {
                {
                    "TestEarth", new Planet {
                        Radius = 61050.39,
                        Center = new Vector3D(0, 0, 0)
                    }
                }
            };

            public static string MissileName = "LIGMA-I";
            public const float TimeStep = 1.0f / 6.0f;

            public static STUFlightController FlightController { get; set; }

            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static IMyRemoteControl RemoteControl { get; set; }
            public static IMyGridProgramRuntimeInfo Runtime { get; set; }

            public static IMyThrust[] Thrusters { get; set; }
            public static IMyGyro[] Gyros { get; set; }
            public static IMyBatteryBlock[] Batteries { get; set; }
            public static IMyGasTank[] GasTanks { get; set; }
            public static IMyWarhead[] Warheads { get; set; }

            /// <summary>
            /// Missile's current fuel level in liters
            /// </summary>
            public static double CurrentFuel { get; set; }
            /// <summary>
            /// Missile's current power level in kilowatt-hours
            /// </summary>
            public static double CurrentPower { get; set; }
            /// <summary>
            /// Missile's total fuel capacity in liters
            /// </summary>
            public static double FuelCapacity { get; set; }
            /// <summary>
            /// Missile's total power capacity in kilowatt-hours
            /// </summary>
            public static double PowerCapacity { get; set; }

            public LIGMA(STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime) {
                Me = me;
                Broadcaster = broadcaster;
                Runtime = runtime;

                LoadRemoteController(grid);
                LoadThrusters(grid);
                LoadGyros(grid);
                LoadBatteries(grid);
                LoadFuelTanks(grid);
                LoadWarheads(grid);

                MeasureTotalPowerCapacity();
                MeasureTotalFuelCapacity();
                MeasureCurrentFuel();
                MeasureCurrentPower();

                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "ALL SYSTEMS NOMINAL",
                    Type = STULogType.OK,
                });

                FlightController = new STUFlightController(RemoteControl, TimeStep, Thrusters, Gyros, Broadcaster);
            }

            private static void LoadRemoteController(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> remoteControlBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyRemoteControl>(remoteControlBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (remoteControlBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = MissileName,
                        Message = "No remote control found on grid",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No remote control found on grid.");
                }
                RemoteControl = remoteControlBlocks[0] as IMyRemoteControl;
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Remote control... nominal",
                    Type = STULogType.OK,
                });
            }

            private static void LoadThrusters(IMyGridTerminalSystem grid) {

                List<IMyTerminalBlock> thrusterBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyThrust>(thrusterBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (thrusterBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = MissileName,
                        Message = "No thrusters found on grid",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No thrusters found on grid.");
                }

                IMyThrust[] allThrusters = new IMyThrust[thrusterBlocks.Count];

                for (int i = 0; i < thrusterBlocks.Count; i++) {
                    allThrusters[i] = thrusterBlocks[i] as IMyThrust;
                }

                Broadcaster.Log(new STULog {
                    Sender = "LIGMA-I",
                    Message = "Thrusters... nominal",
                    Type = STULogType.OK,
                });

                Thrusters = allThrusters;
            }

            private static void LoadGyros(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> gyroBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGyro>(gyroBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gyroBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = MissileName,
                        Message = "No gyros found on grid",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No thrusters found on grid.");
                }
                IMyGyro[] gyros = new IMyGyro[gyroBlocks.Count];
                for (int i = 0; i < gyroBlocks.Count; i++) {
                    gyros[i] = gyroBlocks[i] as IMyGyro;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Gyros... nominal",
                    Type = STULogType.OK,
                });
                Gyros = gyros;
            }

            private static void LoadBatteries(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> batteryBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (batteryBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = MissileName,
                        Message = "No batteries found on grid",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No batteries found on grid.");
                }
                IMyBatteryBlock[] batteries = new IMyBatteryBlock[batteryBlocks.Count];
                for (int i = 0; i < batteryBlocks.Count; i++) {
                    batteries[i] = batteryBlocks[i] as IMyBatteryBlock;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Batteries... nominal",
                    Type = STULogType.OK,
                });
                Batteries = batteries;
            }

            private static void LoadFuelTanks(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> gasTankBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGasTank>(gasTankBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gasTankBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = MissileName,
                        Message = "No fuel tanks found on grid",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No fuel tanks found on grid.");
                }
                IMyGasTank[] fuelTanks = new IMyGasTank[gasTankBlocks.Count];
                for (int i = 0; i < gasTankBlocks.Count; i++) {
                    fuelTanks[i] = gasTankBlocks[i] as IMyGasTank;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Fuel tanks... nominal",
                    Type = STULogType.OK,
                });
                GasTanks = fuelTanks;
            }

            private static void LoadWarheads(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> warheadBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyWarhead>(warheadBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (warheadBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = MissileName,
                        Message = "No warheads found on grid.",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No warheads found on grid");
                }
                IMyWarhead[] warheads = new IMyWarhead[warheadBlocks.Count];
                for (int i = 0; i < warheadBlocks.Count; i++) {
                    warheads[i] = warheadBlocks[i] as IMyWarhead;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Warheads... nominal",
                    Type = STULogType.OK,
                });
                Warheads = warheads;
            }

            private static void MeasureTotalPowerCapacity() {
                double capacity = 0;
                foreach (IMyBatteryBlock battery in Batteries) {
                    capacity += battery.MaxStoredPower * 1000;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = $"Total power capacity: {capacity} kWh",
                    Type = STULogType.OK,
                });
                PowerCapacity = capacity;
            }

            private static void MeasureTotalFuelCapacity() {
                double capacity = 0;
                foreach (IMyGasTank tank in GasTanks) {
                    capacity += tank.Capacity;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = $"Total fuel capacity: {capacity} L",
                    Type = STULogType.OK,
                });
                FuelCapacity = capacity;
            }

            private static void MeasureCurrentFuel() {
                double currentFuel = 0;
                foreach (IMyGasTank tank in GasTanks) {
                    currentFuel += tank.FilledRatio * tank.Capacity;
                }
                CurrentFuel = currentFuel;
            }

            private static void MeasureCurrentPower() {
                double currentPower = 0;
                foreach (IMyBatteryBlock battery in Batteries) {
                    currentPower += battery.CurrentStoredPower * 1000;
                }
                CurrentPower = currentPower;
            }

            public static void UpdateMeasurements() {
                FlightController.Update();
                MeasureCurrentFuel();
                MeasureCurrentPower();
            }

            public static void PingMissionControl() {
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
            }

            public static void ArmWarheads() {
                Array.ForEach(Warheads, warhead => {
                    warhead.IsArmed = true;
                });
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "WARHEADS ARMED",
                    Type = STULogType.ERROR,
                    Metadata = GetTelemetryDictionary()
                });
            }

            public static void SelfDestruct() {
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "It was fun while it lasted",
                    Type = STULogType.WARNING,
                    Metadata = GetTelemetryDictionary()
                });
                foreach (IMyWarhead warhead in Warheads) {
                    warhead.IsArmed = true;
                }
                foreach (IMyWarhead warhead in Warheads) {
                    warhead.Detonate();
                }
            }

            public static Dictionary<string, string> GetTelemetryDictionary() {
                return new Dictionary<string, string> {
                    { "VelocityMagnitude", FlightController.VelocityMagnitude.ToString() },
                    { "VelocityComponents", FlightController.VelocityComponents.ToString() },
                    { "CurrentFuel", CurrentFuel.ToString() },
                    { "CurrentPower", CurrentPower.ToString() },
                    { "FuelCapacity", FuelCapacity.ToString() },
                    { "PowerCapacity", PowerCapacity.ToString() },
                };
            }

            public static void CreateErrorBroadcast(string message) {
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = $"FATAL - {message}",
                    Type = STULogType.ERROR,
                    Metadata = GetTelemetryDictionary()
                });
                throw new Exception(message);
            }

        }
    }
}
