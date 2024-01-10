﻿using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public static MyDetectedEntityInfo TargetData { get; set; }
            public static Vector3D LaunchCoordinates { get; set; }

            public static Planet? TargetPlanet { get; set; }
            public static Planet? LaunchPlanet { get; set; }

            public const float TimeStep = 1.0f / 6.0f;

            public static STUFlightController FlightController { get; set; }

            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static IMyShipConnector Connector { get; set; }
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
                LoadConnector(grid);

                MeasureTotalPowerCapacity();
                MeasureTotalFuelCapacity();
                MeasureCurrentFuel();
                MeasureCurrentPower();

                CreateOkBroadcast("ALL SYSTEMS GO");

                FlightController = new STUFlightController(RemoteControl, TimeStep, Thrusters, Gyros);
                LaunchCoordinates = FlightController.CurrentPosition;
            }

            private static void LoadRemoteController(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> remoteControlBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyRemoteControl>(remoteControlBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (remoteControlBlocks.Count == 0) {
                    CreateFatalErrorBroadcast("No remote control blocks found on grid");
                }
                RemoteControl = remoteControlBlocks[0] as IMyRemoteControl;
                CreateOkBroadcast("Remote control... nominal");
            }

            private static void LoadThrusters(IMyGridTerminalSystem grid) {

                List<IMyTerminalBlock> thrusterBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyThrust>(thrusterBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (thrusterBlocks.Count == 0) {
                    CreateFatalErrorBroadcast("No thrusters found on grid");
                }

                IMyThrust[] allThrusters = new IMyThrust[thrusterBlocks.Count];

                for (int i = 0; i < thrusterBlocks.Count; i++) {
                    allThrusters[i] = thrusterBlocks[i] as IMyThrust;
                }

                CreateOkBroadcast("Thrusters... nominal");
                Thrusters = allThrusters;
            }

            private static void LoadGyros(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> gyroBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGyro>(gyroBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gyroBlocks.Count == 0) {
                    CreateFatalErrorBroadcast("No gyros found on grid");
                }
                IMyGyro[] gyros = new IMyGyro[gyroBlocks.Count];
                for (int i = 0; i < gyroBlocks.Count; i++) {
                    gyros[i] = gyroBlocks[i] as IMyGyro;
                }
                CreateOkBroadcast("Gyros... nominal");
                Gyros = gyros;
            }

            private static void LoadBatteries(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> batteryBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (batteryBlocks.Count == 0) {
                    CreateFatalErrorBroadcast("No batteries found on grid");
                }
                IMyBatteryBlock[] batteries = new IMyBatteryBlock[batteryBlocks.Count];
                for (int i = 0; i < batteryBlocks.Count; i++) {
                    batteries[i] = batteryBlocks[i] as IMyBatteryBlock;
                }
                CreateOkBroadcast("Batteries... nominal");
                Batteries = batteries;
            }

            private static void LoadFuelTanks(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> gasTankBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGasTank>(gasTankBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gasTankBlocks.Count == 0) {
                    CreateFatalErrorBroadcast("No fuel tanks found on grid");
                }
                IMyGasTank[] fuelTanks = new IMyGasTank[gasTankBlocks.Count];
                for (int i = 0; i < gasTankBlocks.Count; i++) {
                    fuelTanks[i] = gasTankBlocks[i] as IMyGasTank;
                }
                CreateOkBroadcast("Fuel tanks... nominal");
                GasTanks = fuelTanks;
            }

            private static void LoadWarheads(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> warheadBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyWarhead>(warheadBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (warheadBlocks.Count == 0) {
                    CreateFatalErrorBroadcast("No warheads found on grid");
                }
                IMyWarhead[] warheads = new IMyWarhead[warheadBlocks.Count];
                for (int i = 0; i < warheadBlocks.Count; i++) {
                    warheads[i] = warheadBlocks[i] as IMyWarhead;
                }
                CreateOkBroadcast("Warheads... nominal");
                Warheads = warheads;
            }

            private static void LoadConnector(IMyGridTerminalSystem grid) {
                var connector = grid.GetBlockWithName("LIGMA Connector");
                if (connector == null) {
                    CreateFatalErrorBroadcast("Either no connectors found, or too many connectors found. Only one allowed.");
                }
                CreateOkBroadcast("Connector... nominal");
                Connector = connector as IMyShipConnector;
            }

            private static void MeasureTotalPowerCapacity() {
                double capacity = 0;
                foreach (IMyBatteryBlock battery in Batteries) {
                    capacity += battery.MaxStoredPower * 1000;
                }
                CreateOkBroadcast($"Total power capacity: {capacity} kWh");
                PowerCapacity = capacity;
            }

            private static void MeasureTotalFuelCapacity() {
                double capacity = 0;
                foreach (IMyGasTank tank in GasTanks) {
                    capacity += tank.Capacity;
                }
                CreateOkBroadcast($"Total fuel capacity: {capacity} L");
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

            public static void SendTelemetry() {
                // Empty message means pure telemetry message
                Broadcaster.Log(new STULog {
                    Sender = LIGMA_VARIABLES.LIGMA_VEHICLE_NAME,
                    Message = "",
                    Type = STULogType.INFO,
                    Metadata = GetTelemetryDictionary(),
                });
            }

            public static void ArmWarheads() {
                foreach (IMyWarhead warhead in Warheads) {
                    warhead.IsArmed = true;
                }
                CreateWarningBroadcast("WARHEADS ARMED");
            }

            public static void SelfDestruct() {
                CreateErrorBroadcast("SELF DESTRUCT INITIATED");
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

            public static void CreateFatalErrorBroadcast(string message) {
                CreateBroadcast($"FATAL -- {message}", STULogType.ERROR);
                throw new Exception(message);
            }

            public static void CreateErrorBroadcast(string message) {
                CreateBroadcast(message, STULogType.ERROR);
            }

            public static void CreateWarningBroadcast(string message) {
                CreateBroadcast(message, STULogType.WARNING);
            }

            public static void CreateOkBroadcast(string message) {
                CreateBroadcast(message, STULogType.OK);
            }

            private static void CreateBroadcast(string message, STULogType type) {
                Broadcaster.Log(new STULog {
                    Sender = LIGMA_VARIABLES.LIGMA_VEHICLE_NAME,
                    Message = message,
                    Type = type,
                });
            }

        }
    }
}
