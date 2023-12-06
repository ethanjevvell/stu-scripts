using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            private const string MissileName = "LIGMA-I";
            public const double TimeStep = 1.0 / 6.0;

            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster Broadcaster { get; set; }
            public static IMyRemoteControl RemoteControl { get; set; }
            public static IMyGridProgramRuntimeInfo Runtime { get; set; }

            public static LIGMAThruster[] AllThrusters { get; set; }
            public static LIGMAThruster[] ForwardThrusters { get; set; }
            public static LIGMAThruster[] ReverseThrusters { get; set; }
            public static LIGMAThruster[] LeftThrusters { get; set; }
            public static LIGMAThruster[] RightThrusters { get; set; }
            public static LIGMAThruster[] UpThrusters { get; set; }
            public static LIGMAThruster[] DownThrusters { get; set; }

            public static double MaximumForwardThrust { get; set; }
            public static double MaximumReverseThrust { get; set; }
            public static double MaximumLeftThrust { get; set; }
            public static double MaximumRightThrust { get; set; }
            public static double MaximumUpThrust { get; set; }
            public static double MaximumDownThrust { get; set; }

            public static LIGMAGyro[] Gyros { get; set; }
            public static LIGMABattery[] Batteries { get; set; }
            public static LIGMAFuelTank[] FuelTanks { get; set; }
            public static LIGMAWarhead[] Warheads { get; set; }
            public static Vector3D StartPosition { get; set; }
            /// <summary>
            /// Position of the missile at the current time in world coordinates
            /// </summary>
            public static Vector3D CurrentPosition { get; set; }
            public static MatrixD CurrentOrientation { get; set; }
            /// <summary>
            /// Position of the missile the last time it was measured in world coordinates
            /// </summary>
            public static Vector3D PreviousPosition { get; set; }
            public static MatrixD PreviousOrientation { get; set; }
            /// <summary>
            /// Missile's current velocity in meters per second, broken down into its components
            /// </summary>
            public static Vector3D VelocityComponents { get; set; }
            /// <summary>
            /// Missile's current velocity in meters per second
            /// </summary>
            public static double VelocityMagnitude { get; set; }
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
            public static double TotalMissileMass { get; set; }

            public Missile(STUMasterLogBroadcaster broadcaster, IMyGridTerminalSystem grid, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo runtime) {
                Me = me;
                Broadcaster = broadcaster;
                Runtime = runtime;

                MaximumForwardThrust = 0;
                MaximumReverseThrust = 0;
                MaximumLeftThrust = 0;
                MaximumRightThrust = 0;
                MaximumUpThrust = 0;
                MaximumDownThrust = 0;

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
                MeasureCurrentPosition();

                CurrentPosition = RemoteControl.GetPosition();
                CurrentOrientation = RemoteControl.WorldMatrix;

                PreviousPosition = CurrentPosition;
                PreviousOrientation = RemoteControl.WorldMatrix;

                StartPosition = CurrentPosition;
                TotalMissileMass = RemoteControl.CalculateShipMass().TotalMass;

                MeasureCurrentVelocity();
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
                    Metadata = GetTelemetryDictionary()
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

                LIGMAThruster[] allThrusters = new LIGMAThruster[thrusterBlocks.Count];

                for (int i = 0; i < thrusterBlocks.Count; i++) {
                    allThrusters[i] = new LIGMAThruster(thrusterBlocks[i] as IMyThrust);
                }

                AssignThrustersByOrientation(allThrusters);

                Broadcaster.Log(new STULog {
                    Sender = "LIGMA-I",
                    Message = "Thrusters... nominal",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });

                AllThrusters = allThrusters;
            }

            private static void AssignThrustersByOrientation(LIGMAThruster[] allThrusters) {

                int forwardCount = 0;
                int reverseCount = 0;
                int leftCount = 0;
                int rightCount = 0;
                int upCount = 0;
                int downCount = 0;

                foreach (LIGMAThruster thruster in allThrusters) {

                    MyBlockOrientation thrusterDirection = thruster.Thruster.Orientation;

                    if (thrusterDirection.Forward == Base6Directions.Direction.Forward) {
                        forwardCount++;
                    }

                    if (thrusterDirection.Forward == Base6Directions.Direction.Backward) {
                        reverseCount++;
                    }

                    // in-game geometry is the reverse of what you'd expect for left-right
                    if (thrusterDirection.Forward == Base6Directions.Direction.Right) {
                        leftCount++;
                    }

                    // in-game geometry is the reverse of what you'd expect for left-right
                    if (thrusterDirection.Forward == Base6Directions.Direction.Left) {
                        rightCount++;
                    }

                    if (thrusterDirection.Forward == Base6Directions.Direction.Up) {
                        upCount++;
                    }

                    if (thrusterDirection.Forward == Base6Directions.Direction.Down) {
                        downCount++;
                    }

                }

                ForwardThrusters = new LIGMAThruster[forwardCount];
                ReverseThrusters = new LIGMAThruster[reverseCount];
                LeftThrusters = new LIGMAThruster[leftCount];
                RightThrusters = new LIGMAThruster[rightCount];
                UpThrusters = new LIGMAThruster[upCount];
                DownThrusters = new LIGMAThruster[downCount];

                forwardCount = 0;
                reverseCount = 0;
                leftCount = 0;
                rightCount = 0;
                upCount = 0;
                downCount = 0;

                foreach (LIGMAThruster thruster in allThrusters) {

                    MyBlockOrientation thrusterDirection = thruster.Thruster.Orientation;

                    if (thrusterDirection.Forward == Base6Directions.Direction.Forward) {
                        ForwardThrusters[forwardCount] = thruster;
                        MaximumForwardThrust += ForwardThrusters[forwardCount].Thruster.MaxThrust;
                        forwardCount++;
                    }

                    if (thrusterDirection.Forward == Base6Directions.Direction.Backward) {
                        ReverseThrusters[reverseCount] = thruster;
                        MaximumReverseThrust += ReverseThrusters[reverseCount].Thruster.MaxThrust;
                        reverseCount++;
                    }

                    // in-game geometry is the reverse of what you'd expect for left-right
                    if (thrusterDirection.Forward == Base6Directions.Direction.Right) {
                        LeftThrusters[leftCount] = thruster;
                        MaximumLeftThrust += LeftThrusters[leftCount].Thruster.MaxThrust;
                        leftCount++;
                    }

                    // in-game geometry is the reverse of what you'd expect for left-right
                    if (thrusterDirection.Forward == Base6Directions.Direction.Left) {
                        RightThrusters[rightCount] = thruster;
                        MaximumRightThrust += RightThrusters[rightCount].Thruster.MaxThrust;
                        rightCount++;
                    }

                    if (thrusterDirection.Forward == Base6Directions.Direction.Up) {
                        UpThrusters[upCount] = thruster;
                        MaximumUpThrust += UpThrusters[upCount].Thruster.MaxThrust;
                        upCount++;
                    }

                    if (thrusterDirection.Forward == Base6Directions.Direction.Down) {
                        DownThrusters[downCount] = thruster;
                        MaximumDownThrust += DownThrusters[downCount].Thruster.MaxThrust;
                        downCount++;
                    }

                }

            }

            private static void SetThrusterOverrides(LIGMAThruster[] thrusters, double overrideValue) {
                Array.ForEach(thrusters, thruster => thruster.SetThrust(overrideValue));
            }

            private static void ToggleThrusters(LIGMAThruster[] thrusters, bool onOrOff) {
                Array.ForEach(thrusters, thruster => thruster.Thruster.Enabled = onOrOff);
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
                Vector3D worldVelocity = (CurrentPosition - PreviousPosition) / Runtime.TimeSinceLastRun.TotalSeconds;
                Vector3D localVelocity = Vector3D.TransformNormal(worldVelocity, MatrixD.Transpose(PreviousOrientation));
                // Space Engineers considers the missile's forward direction (the direction it's facing) to be in the negative Z direction
                // We reverse that by convention because it's easier to think about
                VelocityComponents = localVelocity * new Vector3D(1, 1, -1);
                VelocityMagnitude = Vector3D.Distance(PreviousPosition, CurrentPosition) / Runtime.TimeSinceLastRun.TotalSeconds;
            }

            private static void MeasureCurrentPosition() {
                CurrentPosition = Me.GetPosition();
            }

            public static void UpdateMeasurements() {
                MeasureCurrentPosition();
                MeasureCurrentVelocity();
                MeasureCurrentFuel();
                MeasureCurrentPower();

                // Ensure current position is saved as the "previous position" of the next time Main() runs
                PreviousOrientation = CurrentOrientation;
                PreviousPosition = CurrentPosition;
            }

            public static void PingMissionControl() {
                Broadcaster.Log(new STULog {
                    Sender = MissileName,
                    Message = "",
                    Type = STULogType.OK,
                    Metadata = GetTelemetryDictionary()
                });
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
                    { "VelocityMagnitude", VelocityMagnitude.ToString() },
                    { "VelocityComponents", VelocityComponents.ToString() },
                    { "CurrentFuel", CurrentFuel.ToString() },
                    { "CurrentPower", CurrentPower.ToString() },
                    { "FuelCapacity", FuelCapacity.ToString() },
                    { "PowerCapacity", PowerCapacity.ToString() },
                };
            }

        }
    }
}
