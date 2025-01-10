using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public static bool IS_STAGED_LIGMA = false;

            public static MyDetectedEntityInfo TargetData { get; set; }
            public static Vector3D LaunchCoordinates { get; set; }

            public static STUGalacticMap.Planet? TargetPlanet { get; set; }
            public static STUGalacticMap.Planet? LaunchPlanet { get; set; }

            public const float TimeStep = 1.0f / 6.0f;
            public static float Timestamp = 0;

            public static Phase CurrentPhase = Phase.Idle;

            public static Stage LaunchStage { get; set; }
            public static Stage FlightStage { get; set; }
            public static Stage TerminalStage { get; set; }

            public static STUFlightController FlightController { get; set; }
            public static IMySensorBlock DetonationSensor { get; set; }
            public static STURaycaster Raycaster { get; set; }
            public static STUFlightController.STUInterceptCalculator InterceptCalculator { get; set; }

            public static IMyProgrammableBlock Me { get; set; }
            public static STUMasterLogBroadcaster s_telemetryBroadcaster { get; set; }
            public static STUMasterLogBroadcaster s_logBroadcaster { get; set; }
            public static IMyRemoteControl RemoteControl { get; set; }
            public static IMyGridProgramRuntimeInfo Runtime { get; set; }

            public static IMyThrust[] AllThrusters { get; set; }
            public static IMyGyro[] Gyros { get; set; }
            public static IMyBatteryBlock[] Batteries { get; set; }
            public static IMyGasTank[] GasTanks { get; set; }
            public static IMyWarhead[] Warheads { get; set; }
            public static IMyShipConnector[] Connectors { get; set; }
            public static IMyShipMergeBlock s_mainMergeBlock { get; set; }

            IEnumerator<bool> _hardwareLoadStateMachine;

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

            public enum Phase {
                Idle,
                Launch,
                Flight,
                Descent,
                Terminal,
            }

            public LIGMA(STUMasterLogBroadcaster telemetryBroadcaster,
                         STUMasterLogBroadcaster logBroadcaster,
                         IMyGridTerminalSystem grid,
                         IMyProgrammableBlock me,
                         IMyGridProgramRuntimeInfo runtime) {
                Me = me;
                s_telemetryBroadcaster = telemetryBroadcaster;
                s_logBroadcaster = logBroadcaster;
                Runtime = runtime;
            }

            public bool LoadHardware(IMyGridTerminalSystem grid) {

                if (_hardwareLoadStateMachine == null) {
                    _hardwareLoadStateMachine = LoadHardwareCoroutine(grid).GetEnumerator();
                }

                if (!_hardwareLoadStateMachine.MoveNext()) {
                    _hardwareLoadStateMachine.Dispose();
                    _hardwareLoadStateMachine = null;
                    return true;
                }

                return false;

            }

            IEnumerable<bool> LoadHardwareCoroutine(IMyGridTerminalSystem grid) {

                while (!LoadRemoteController(grid)) {
                    yield return true;
                }

                while (!LoadBatteries(grid)) {
                    yield return true;
                }

                while (!LoadFuelTanks(grid)) {
                    yield return true;
                }

                while (!LoadWarheads(grid)) {
                    yield return true;
                }

                while (!LoadConnectors(grid)) {
                    yield return true;
                }

                while (!LoadMainMergeBlock(grid)) {
                    yield return true;
                }

                while (!LoadDetonationSensor(grid)) {
                    yield return true;
                }

                MeasureTotalPowerCapacity();
                MeasureTotalFuelCapacity();
                MeasureCurrentFuel();
                MeasureCurrentPower();

                foreach (var tank in GasTanks) {
                    tank.Stockpile = true;
                }

                if (IsStagedLIGMA(grid)) {
                    IS_STAGED_LIGMA = true;
                    LaunchStage = new Stage(grid, "LAUNCH");
                    FlightStage = new Stage(grid, "FLIGHT");
                    TerminalStage = new Stage(grid, "TERMINAL");
                    CreateOkBroadcast("Staged LIGMA detected -- All stages nominal");
                    IS_STAGED_LIGMA = true;

                    // Initial conditions for launch stage
                    LaunchStage.ToggleForwardThrusters(true);
                    LaunchStage.ToggleReverseThrusters(true);
                    LaunchStage.ToggleLateralThrusters(false);

                    // Initial conditions for flight stage
                    FlightStage.ToggleForwardThrusters(false);
                    FlightStage.ToggleReverseThrusters(false);
                    FlightStage.ToggleLateralThrusters(true);

                    // Initial conditions for terminal stage
                    TerminalStage.ToggleForwardThrusters(false);
                    TerminalStage.ToggleReverseThrusters(false);
                    TerminalStage.ToggleLateralThrusters(false);
                }

                int i = 0;

                // Wait three seconds to allow construction of all thrusters to finish before script initilaises
                while (i < 18) {
                    i += 1;
                    yield return true;
                }

                FlightController = new STUFlightController(grid, RemoteControl, Me);
                AllThrusters = FlightController.ActiveThrusters;
                LaunchCoordinates = FlightController.CurrentPosition;
                InterceptCalculator = new STUFlightController.STUInterceptCalculator();

                // Keep dampeners on while LIGMA is still on the launch pad; these will be disabled on launch
                RemoteControl.DampenersOverride = true;

                CreateOkBroadcast("ALL SYSTEMS GO");
            }

            private static bool LoadMainMergeBlock(IMyGridTerminalSystem grid) {
                var mergeBlock = grid.GetBlockWithName("Main Merge Block");
                if (mergeBlock == null) {
                    return false;
                }
                s_mainMergeBlock = mergeBlock as IMyShipMergeBlock;
                CreateOkBroadcast("Merge block... nominal");
                return true;
            }

            private static bool LoadRemoteController(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> remoteControlBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyRemoteControl>(remoteControlBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (remoteControlBlocks.Count == 0) {
                    return false;
                }
                RemoteControl = remoteControlBlocks[0] as IMyRemoteControl;
                CreateOkBroadcast("Remote control... nominal");
                return true;
            }

            private static bool LoadBatteries(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> batteryBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (batteryBlocks.Count == 0) {
                    return false;
                }
                IMyBatteryBlock[] batteries = new IMyBatteryBlock[batteryBlocks.Count];
                for (int i = 0; i < batteryBlocks.Count; i++) {
                    batteries[i] = batteryBlocks[i] as IMyBatteryBlock;
                }
                CreateOkBroadcast("Batteries... nominal");
                Batteries = batteries;
                return true;
            }

            private static bool LoadFuelTanks(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> gasTankBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyGasTank>(gasTankBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (gasTankBlocks.Count == 0) {
                    return false;
                }
                IMyGasTank[] fuelTanks = new IMyGasTank[gasTankBlocks.Count];
                for (int i = 0; i < gasTankBlocks.Count; i++) {
                    fuelTanks[i] = gasTankBlocks[i] as IMyGasTank;
                }
                CreateOkBroadcast("Fuel tanks... nominal");
                GasTanks = fuelTanks;
                return true;
            }

            private static bool LoadWarheads(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> warheadBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyWarhead>(warheadBlocks);
                if (warheadBlocks.Count == 0) {
                    return false;
                }
                IMyWarhead[] warheads = new IMyWarhead[warheadBlocks.Count];
                for (int i = 0; i < warheadBlocks.Count; i++) {
                    warheads[i] = warheadBlocks[i] as IMyWarhead;
                }
                CreateOkBroadcast("Warheads... nominal");
                Warheads = warheads;
                return true;
            }

            private static bool LoadConnectors(IMyGridTerminalSystem grid) {
                List<IMyTerminalBlock> connectorBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyShipConnector>(connectorBlocks, block => block.CubeGrid == Me.CubeGrid);
                if (connectorBlocks.Count == 0) {
                    return false;
                }
                IMyShipConnector[] connectors = new IMyShipConnector[connectorBlocks.Count];
                for (int i = 0; i < connectorBlocks.Count; i++) {
                    connectors[i] = connectorBlocks[i] as IMyShipConnector;
                }
                CreateOkBroadcast("Connectors... nominal");
                Connectors = connectors;
                return true;
            }

            private static bool LoadDetonationSensor(IMyGridTerminalSystem grid) {
                var sensor = grid.GetBlockWithName("Detonation Sensor");
                if (sensor == null) {
                    return false;
                }
                CreateOkBroadcast("Detonation sensor... nominal");
                DetonationSensor = sensor as IMySensorBlock;
                // Disable sensor immediately to prevent premature detonation
                DetonationSensor.Enabled = false;

                // Sensor distance configuration
                DetonationSensor.FrontExtend = 7;
                DetonationSensor.BackExtend = 10;
                DetonationSensor.LeftExtend = 10;
                DetonationSensor.RightExtend = 10;
                DetonationSensor.TopExtend = 10;
                DetonationSensor.BottomExtend = 10;

                // Sensor activation configuration
                DetonationSensor.DetectAsteroids = true;
                DetonationSensor.DetectEnemy = true;
                DetonationSensor.DetectFloatingObjects = true;
                DetonationSensor.DetectLargeShips = true;
                DetonationSensor.DetectNeutral = true;
                DetonationSensor.DetectSmallShips = true;
                DetonationSensor.DetectStations = true;
                DetonationSensor.DetectSubgrids = true;

                // Sensor deactivation configuration
                DetonationSensor.DetectFriendly = false;
                DetonationSensor.DetectOwner = false;
                DetonationSensor.DetectPlayers = false;
                return true;
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

            public static void UpdateState() {
                FlightController.UpdateState();
            }

            public static void UpdateMeasurements() {
                if (IS_STAGED_LIGMA) {
                    LaunchStage.MeasureCurrentFuel();
                    FlightStage.MeasureCurrentFuel();
                    TerminalStage.MeasureCurrentFuel();
                }
                MeasureCurrentFuel();
                MeasureCurrentPower();
                if (CurrentPhase != Phase.Idle) {
                    Timestamp += Runtime.TimeSinceLastRun.Milliseconds;
                }
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

            public static void UpdateTargetData(MyDetectedEntityInfo hitInfo) {
                InterceptCalculator.ChaserPosition = FlightController.CurrentPosition;
                // Use our desired velocity for the calculation instead of our actual velocity
                InterceptCalculator.ChaserSpeed = FlightController.TargetVelocity;
                InterceptCalculator.RunnerPosition = hitInfo.Position;
                InterceptCalculator.RunnerVelocity = hitInfo.Velocity;
                var interceptionPoint = InterceptCalculator.InterceptionPoint;

                // return new MyDetectedEntityInfo(entityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
                MyDetectedEntityInfo myDetectedEntityInfo = new MyDetectedEntityInfo(
                    hitInfo.EntityId,
                    hitInfo.Name,
                    hitInfo.Type,
                    // LIGMA "thinks" the target is at the interception point
                    interceptionPoint,
                    hitInfo.Orientation,
                    hitInfo.Velocity,
                    hitInfo.Relationship,
                    hitInfo.BoundingBox,
                    hitInfo.TimeStamp
                );

                TargetData = hitInfo;
                CreateOkBroadcast($"Target data updated: {TargetData.Position}");
            }

            public static Dictionary<string, string> GetTelemetryDictionary() {
                return new Dictionary<string, string> {
                    { "Id", Me.EntityId.ToString() },
                    { "Name", Me.CustomName},
                    { "Timestamp", Timestamp.ToString() },
                    { "Phase", CurrentPhase.ToString() },
                    { "VelocityMagnitude", FlightController.VelocityMagnitude.ToString() },
                    { "VelocityComponents", FlightController.CurrentVelocity_LocalFrame.ToString() },
                    { "AccelerationComponents", FlightController.AccelerationComponents.ToString() },
                    { "CurrentFuel", CurrentFuel.ToString() },
                    { "CurrentPower", CurrentPower.ToString() },
                    { "FuelCapacity", FuelCapacity.ToString() },
                    { "PowerCapacity", PowerCapacity.ToString() },
                };
            }

            public static void CreateFatalErrorBroadcast(string message) {
                CreateLogBroadcast($"FATAL -- {message}", STULogType.ERROR);
                throw new Exception(message);
            }

            public static void CreateErrorBroadcast(string message) {
                CreateLogBroadcast(message, STULogType.ERROR);
            }

            public static void CreateWarningBroadcast(string message) {
                CreateLogBroadcast(message, STULogType.WARNING);
            }

            public static void CreateOkBroadcast(string message) {
                CreateLogBroadcast(message, STULogType.OK);
            }

            private static void CreateLogBroadcast(string message, string type) {
                s_logBroadcaster.Log(new STULog {
                    Sender = LIGMA_VARIABLES.LIGMA_VEHICLE_NAME,
                    Message = message,
                    Type = type,
                });
            }

            public static void SendTelemetry() {
                // Empty message means pure telemetry message
                s_telemetryBroadcaster.Log(new STULog {
                    Sender = LIGMA_VARIABLES.LIGMA_VEHICLE_NAME,
                    Message = "",
                    Type = STULogType.INFO,
                    Metadata = GetTelemetryDictionary(),
                });
            }


            private static bool IsStagedLIGMA(IMyGridTerminalSystem grid) {
                IMyShipMergeBlock mergeBlock = grid.GetBlockWithName("TERMINAL_TO_FLIGHT_MERGE_BLOCK") as IMyShipMergeBlock;
                return mergeBlock != null;
            }


            public static void JettisonLaunchStage() {

                // Prepare jettisoned stage's thrusters
                LaunchStage.ToggleForwardThrusters(false);
                LaunchStage.ToggleLateralThrusters(false);
                LaunchStage.ToggleReverseThrusters(true);
                LaunchStage.TriggerDisenageBurn();

                // Arm stage warheads
                LaunchStage.TriggerDetonationCountdown();

                // Stage separation
                LaunchStage.DisconnectMergeBlock();

                // Remove launch stage thrusters from vehicle memory
                AllThrusters = Stage.RemoveThrusters(AllThrusters, LaunchStage.ForwardThrusters);
                AllThrusters = Stage.RemoveThrusters(AllThrusters, LaunchStage.LateralThrusters);
                AllThrusters = Stage.RemoveThrusters(AllThrusters, LaunchStage.ReverseThrusters);

                // Remove launch stage hydrogen tanks from fuel calculations
                GasTanks = Stage.RemoveHydrogenTanks(GasTanks, LaunchStage.HydrogenTanks);

                // Turn on flight stage thrusters
                FlightStage.ToggleForwardThrusters(true);
                FlightStage.ToggleLateralThrusters(true);
                FlightStage.ToggleReverseThrusters(true);

                // Turn on terminal stage lateral thrusters only
                TerminalStage.ToggleForwardThrusters(false);
                TerminalStage.ToggleReverseThrusters(false);
                TerminalStage.ToggleLateralThrusters(true);

                List<IMyThrust> newActiveThrusters = new List<IMyThrust>();

                foreach (IMyThrust thruster in AllThrusters) {
                    if (thruster.Enabled) {
                        newActiveThrusters.Add(thruster);
                    }
                }

                FlightController.UpdateThrustersAfterGridChange(newActiveThrusters.ToArray());

            }

            public static void JettisonFlightStage() {

                // Prepare jettisoned stage's thrusters
                FlightStage.ToggleForwardThrusters(false);
                FlightStage.ToggleLateralThrusters(false);
                FlightStage.ToggleReverseThrusters(true);
                FlightStage.TriggerDisenageBurn();

                // Arm stage warheads
                FlightStage.TriggerDetonationCountdown();

                // Stage separation
                FlightStage.DisconnectMergeBlock();

                // Remove flight stage thrusters from vehicle memory
                AllThrusters = Stage.RemoveThrusters(AllThrusters, FlightStage.ForwardThrusters);
                AllThrusters = Stage.RemoveThrusters(AllThrusters, FlightStage.LateralThrusters);
                AllThrusters = Stage.RemoveThrusters(AllThrusters, FlightStage.ReverseThrusters);

                // Remove flight stage hydrogen tanks from fuel calculations
                GasTanks = Stage.RemoveHydrogenTanks(GasTanks, FlightStage.HydrogenTanks);

                // Turn on terminal stage thrusters
                TerminalStage.ToggleForwardThrusters(true);
                TerminalStage.ToggleLateralThrusters(true);
                TerminalStage.ToggleReverseThrusters(true);

                List<IMyThrust> newActiveThrusters = new List<IMyThrust>();

                foreach (IMyThrust thruster in AllThrusters) {
                    if (thruster.Enabled) {
                        newActiveThrusters.Add(thruster);
                    }
                }

                FlightController.UpdateThrustersAfterGridChange(newActiveThrusters.ToArray());

            }

        }
    }
}
