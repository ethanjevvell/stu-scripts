using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            private const string MissileName = "LIGMA-I";

            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static IMyGridProgramRuntimeInfo Runtime { get; set; }
            public static LIGMAThruster[] Thrusters { get; set; }
            public static LIGMAGyro[] Gyros { get; set; }
            public static LIGMABattery[] Batteries { get; set; }
            public static LIGMAFuelTank[] FuelTanks { get; set; }
            public static LIGMAWarhead[] Warheads { get; set; }
            public static Vector3D StartPosition { get; set; }
            /// <summary>
            /// Position of the missile at the current time in world coordinates
            /// </summary>
            public static Vector3D CurrentPosition { get; set; }
            /// <summary>
            /// Position of the missile the last time it was measured in world coordinates
            /// </summary>
            public static Vector3D PreviousPosition { get; set; }
            /// <summary>
            /// Missile's current fuel level in liters
            /// </summary>
            public static double CurrentFuel { get; set; }
            /// <summary>
            /// Missile's current power level in kilowatt-hours
            /// </summary>
            public static double CurrentPower { get; set; }
            /// <summary>
            /// Missile's current velocity in meters per second
            /// </summary>
            public static double Velocity { get; set; }
            /// <summary>
            /// Missile's total fuel capacity in liters
            /// </summary>
            public static double FuelCapacity { get; set; }
            /// <summary>
            /// Missile's total power capacity in kilowatt-hours
            /// </summary>
            public static double PowerCapacity { get; set; }
            /// <summary>
            /// Only used for telemetry; does NOT control any FSM's
            /// </summary>

            public Missile(STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime) {
                Me = me;
                Broadcaster = broadcaster;
                Runtime = runtime;

                LoadThrusters(grid);
                LoadGyros(grid);
                LoadBatteries(grid);
                LoadFuelTanks(grid);
                LoadWarheads(grid);

                MeasureTotalPowerCapacity();
                MeasureTotalFuelCapacity();
                MeasureCurrentFuel();
                MeasureCurrentPower();
                MeasureCurrentPosition();

                CurrentPosition = Me.GetPosition();
                PreviousPosition = CurrentPosition;
                StartPosition = CurrentPosition;

                MeasureCurrentVelocity();
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
                LIGMAThruster[] thrusters = new LIGMAThruster[thrusterBlocks.Count];
                for (int i = 0; i < thrusterBlocks.Count; i++) {
                    thrusters[i] = new LIGMAThruster(thrusterBlocks[i] as IMyThrust);
                }
                Broadcaster.Log(new STULog {
                    Sender = "LIGMA-I",
                    Message = "Thrusters... nominal",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
                Thrusters = thrusters;
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
                LIGMAGyro[] gyros = new LIGMAGyro[gyroBlocks.Count];
                for (int i = 0; i < gyroBlocks.Count; i++) {
                    gyros[i] = new LIGMAGyro(gyroBlocks[i] as IMyGyro);
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Gyros... nominal",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
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
                LIGMABattery[] batteries = new LIGMABattery[batteryBlocks.Count];
                for (int i = 0; i < batteryBlocks.Count; i++) {
                    batteries[i] = new LIGMABattery(batteryBlocks[i] as IMyBatteryBlock);
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Batteries... nominal",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
                Batteries = batteries;
            }

            private static void LoadFuelTanks(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> fuelTankBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGasTank>(fuelTankBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (fuelTankBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = MissileName,
                        Message = "No fuel tanks found on grid",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No fuel tanks found on grid.");
                }
                LIGMAFuelTank[] fuelTanks = new LIGMAFuelTank[fuelTankBlocks.Count];
                for (int i = 0; i < fuelTankBlocks.Count; i++) {
                    fuelTanks[i] = new LIGMAFuelTank(fuelTankBlocks[i] as IMyGasTank);
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Fuel tanks... nominal",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
                FuelTanks = fuelTanks;
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
                LIGMAWarhead[] warheads = new LIGMAWarhead[warheadBlocks.Count];
                for (int i = 0; i < warheadBlocks.Count; i++) {
                    warheads[i] = new LIGMAWarhead(warheadBlocks[i] as IMyWarhead);
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Warheads... nominal",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
                Warheads = warheads;
            }

            private static void MeasureTotalPowerCapacity() {
                double capacity = 0;
                foreach (LIGMABattery battery in Batteries) {
                    capacity += battery.Battery.MaxStoredPower * 1000;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = $"Total power capacity: {capacity} kWh",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
                PowerCapacity = capacity;
            }

            private static void MeasureTotalFuelCapacity() {
                double capacity = 0;
                foreach (LIGMAFuelTank tank in FuelTanks) {
                    capacity += tank.Tank.Capacity;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = $"Total fuel capacity: {capacity} L",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
                FuelCapacity = capacity;
            }

            private static void MeasureCurrentFuel() {
                double currentFuel = 0;
                foreach (LIGMAFuelTank tank in FuelTanks) {
                    currentFuel += tank.Tank.FilledRatio * tank.Tank.Capacity;
                }
                CurrentFuel = currentFuel;
            }

            private static void MeasureCurrentPower() {
                double currentPower = 0;
                foreach (LIGMABattery battery in Batteries) {
                    currentPower += battery.Battery.CurrentStoredPower * 1000;
                }
                CurrentPower = currentPower;
            }

            private static void MeasureCurrentVelocity() {
                PreviousPosition = CurrentPosition;
                MeasureCurrentPosition();
                Velocity = Vector3D.Distance(PreviousPosition, CurrentPosition) / Runtime.TimeSinceLastRun.TotalSeconds;
            }

            private static void MeasureCurrentPosition() {
                CurrentPosition = Me.GetPosition();
            }

            private static void UpdateMeasurements() {
                MeasureCurrentVelocity();
                MeasureCurrentFuel();
                MeasureCurrentPower();
            }

            public static void PingMissionControl() {
                UpdateMeasurements();
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
            }

            public static void ToggleThrusters(bool onOrOff) {
                foreach (LIGMAThruster thruster in Thrusters) {
                    thruster.Thruster.Enabled = onOrOff;
                }
            }

            public static void ToggleGyros(bool onOrOff) {
                foreach (LIGMAGyro gyro in Gyros) {
                    gyro.Gyro.Enabled = onOrOff;
                }
            }

            public static void SelfDestruct() {
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "It was fun while it lasted",
                    Type = STULogType.WARNING,
                    Metadata = GetTelemetryDictionary()
                });
                foreach (LIGMAWarhead warhead in Warheads) {
                    warhead.Arm();
                }
                foreach (LIGMAWarhead warhead in Warheads) {
                    warhead.Detonate();
                }
            }

            public static Dictionary<string, string> GetTelemetryDictionary() {
                return new Dictionary<string, string> {
                    { "Velocity", Velocity.ToString() },
                    { "CurrentFuel", CurrentFuel.ToString() },
                    { "CurrentPower", CurrentPower.ToString() },
                    { "FuelCapacity", FuelCapacity.ToString() },
                    { "PowerCapacity", PowerCapacity.ToString() },
                };
            }

        }
    }
}
