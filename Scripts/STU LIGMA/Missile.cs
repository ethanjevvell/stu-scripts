using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public class Missile {

            private const string MissileName = "LIGMA-I";

            public IMyProgrammableBlock Me { get; set; }
            public STUMasterLogBroadcaster Broadcaster { get; set; }
            public STUThruster[] Thrusters { get; set; }
            public STUGyro[] Gyros { get; set; }
            public STUBattery[] Batteries { get; set; }
            public STUFuelTank[] FuelTanks { get; set; }

            /// <summary>
            /// Missile's current fuel level in liters
            /// </summary>
            public double CurrentFuel { get; set; }
            /// <summary>
            /// Missile's current power level in kilowatt-hours
            /// </summary>
            public double CurrentPower { get; set; }

            /// <summary>
            /// Missile's total fuel capacity in liters
            /// </summary>
            private double FuelCapacity { get; set; }
            /// <summary>
            /// Missile's total power capacity in kilowatt-hours
            /// </summary>
            private double PowerCapacity { get; set; }

            public Missile(STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me) {
                Me = me;
                Broadcaster = broadcaster;
                Thrusters = LoadThrusters(grid);
                Gyros = LoadGyros(grid);
                Batteries = LoadBatteries(grid);
                FuelTanks = LoadFuelTanks(grid);
                PowerCapacity = MeasureTotalPowerCapacity();
                FuelCapacity = MeasureTotalFuelCapacity();
                CurrentFuel = MeasureCurrentFuel();
                CurrentPower = MeasureCurrentPower();
            }

            public STUThruster[] LoadThrusters(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> thrusterBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyThrust>(thrusterBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (thrusterBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = "LIGMA-I",
                        Message = "No thrusters found on grid",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No thrusters found on grid.");
                }
                STUThruster[] thrusters = new STUThruster[thrusterBlocks.Count];
                for (int i = 0; i < thrusterBlocks.Count; i++) {
                    thrusters[i] = new STUThruster(thrusterBlocks[i] as IMyThrust);
                }
                Broadcaster.Log(new STULog {
                    Sender = "LIGMA-I",
                    Message = "Thrusters... nominal",
                    Type = STULogType.OK
                });
                return thrusters;
            }

            public STUGyro[] LoadGyros(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> gyroBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGyro>(gyroBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gyroBlocks.Count == 0) {
                    Broadcaster.Log(new STULog {
                        Sender = "LIGMA-I",
                        Message = "No gyros found on grid",
                        Type = STULogType.ERROR
                    });
                    throw new Exception("No thrusters found on grid.");
                }
                STUGyro[] gyros = new STUGyro[gyroBlocks.Count];
                for (int i = 0; i < gyroBlocks.Count; i++) {
                    gyros[i] = new STUGyro(gyroBlocks[i] as IMyGyro);
                }
                Broadcaster.Log(new STULog {
                    Sender = "LIGMA-I",
                    Message = "Gyros... nominal",
                    Type = STULogType.OK
                });
                return gyros;
            }

            public STUBattery[] LoadBatteries(IMyGridTerminalSystem grid) {
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
                STUBattery[] batteries = new STUBattery[batteryBlocks.Count];
                for (int i = 0; i < batteryBlocks.Count; i++) {
                    batteries[i] = new STUBattery(batteryBlocks[i] as IMyBatteryBlock);
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Batteries... nominal",
                    Type = STULogType.OK
                });
                return batteries;
            }

            public STUFuelTank[] LoadFuelTanks(IMyGridTerminalSystem grid) {
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
                STUFuelTank[] fuelTanks = new STUFuelTank[fuelTankBlocks.Count];
                for (int i = 0; i < fuelTankBlocks.Count; i++) {
                    fuelTanks[i] = new STUFuelTank(fuelTankBlocks[i] as IMyGasTank);
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "Fuel tanks... nominal",
                    Type = STULogType.OK
                });
                return fuelTanks;
            }

            private double MeasureTotalPowerCapacity() {
                double capacity = 0;
                foreach (STUBattery battery in Batteries) {
                    capacity += battery.Battery.MaxStoredPower * 1000;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = $"Total power capacity: {capacity} kWh",
                    Type = STULogType.OK
                });
                return capacity;
            }

            private double MeasureTotalFuelCapacity() {
                double capacity = 0;
                foreach (STUFuelTank tank in FuelTanks) {
                    capacity += tank.Tank.Capacity;
                }
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = $"Total fuel capacity: {capacity} L",
                    Type = STULogType.OK
                });
                return capacity;
            }

            public double MeasureCurrentFuel() {
                double currentFuel = 0;
                foreach (STUFuelTank tank in FuelTanks) {
                    currentFuel += tank.Tank.FilledRatio * tank.Tank.Capacity;
                }
                return currentFuel;
            }

            public double MeasureCurrentPower() {
                double currentPower = 0;
                foreach (STUBattery battery in Batteries) {
                    currentPower += battery.Battery.CurrentStoredPower * 1000;
                }
                return currentPower;
            }

            public void PingMissionControl() {

            }

            public void ToggleThrusters(bool onOrOff) {
                foreach (STUThruster thruster in Thrusters) {
                    thruster.Thruster.Enabled = onOrOff;
                }
            }

            public void ToggleGyros(bool onOrOff) {
                foreach (STUGyro gyro in Gyros) {
                    gyro.Gyro.Enabled = onOrOff;
                }
            }
        }
    }
}
