using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {

    partial class Program : MyGridProgram {

        bool ISSUED_PARACHUTE_WARNING = false;
        bool EXECUTED_EMERGENCY_LANDING = false;

        MyIni _ini = new MyIni();

        static string _minerName;
        double _cruiseAltitude;
        double _cruiseVelocity;
        double _ascendVelocity;
        double _descendVelocity;
        bool _debug;

        IMyUnicastListener _droneListener;

        static STUMasterLogBroadcaster s_telemetryBroadcaster { get; set; }
        static STUMasterLogBroadcaster s_logBroadcaster { get; set; }
        STUFlightController _flightController { get; set; }
        STURaycaster _raycaster { get; set; }
        IMyRemoteControl _remoteControl { get; set; }

        IMyShipConnector _droneConnector { get; set; }
        List<IMyCargoContainer> _droneCargoContainers = new List<IMyCargoContainer>();

        IMyShipConnector _homeBaseConnector { get; set; }
        List<IMyCargoContainer> _baseCargoContainers = new List<IMyCargoContainer>();

        string MinerMainState {
            get { return _minerMainState; }
            set {
                _minerMainState = value;
                CreateInfoBroadcast($"Transitioning to {value}");
                _flightController.UpdateShipMass();
                SetStatusLights(value);
            }
        }
        string _minerMainState;

        List<IMyShipDrill> _drills = new List<IMyShipDrill>();
        List<IMyGasTank> _hydrogenTanks = new List<IMyGasTank>();
        List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
        List<IMyLightingBlock> _statusLights = new List<IMyLightingBlock>();

        static LogLCD _logScreen { get; set; }

        DrillRoutine _drillRoutineStateMachine { get; set; }
        STUInventoryEnumerator _inventoryEnumerator { get; set; }
        static MiningDroneData s_droneData { get; set; }
        string _minerId { get; set; }

        STULog _tempFlightLog;
        STULog _tempOutgoingLog = new STULog();
        Dictionary<string, string> _tempMetadataDictionary = new Dictionary<string, string>();
        Dictionary<string, double> _tempInventoryEnumeratorDictionary = new Dictionary<string, double>();
        List<MyInventoryItem> _tempInventoryItems = new List<MyInventoryItem>();

        // Damage monitoring
        STUDamageMonitor _damageMonitor;
        List<IMyCubeBlock> _tempDamagedBlocks = new List<IMyCubeBlock>();

        // Emergency systems
        List<IMyParachute> _parachutes = new List<IMyParachute>();
        IMyBeacon _emergencyBeacon { get; set; }

        public Program() {

            try {
                ParseMinerConfiguration(Me.CustomData);
                _minerId = Me.EntityId.ToString();

                // Get blocks needed for various systems
                _statusLights = GetStatusLights();
                _remoteControl = GetRemoteControl();
                _droneConnector = GetConnector();
                _emergencyBeacon = GetEmergencyBeacon();
                _parachutes = GetParachutes();

                GridTerminalSystem.GetBlocksOfType(_drills, (block) => block.CubeGrid == Me.CubeGrid);
                GridTerminalSystem.GetBlocksOfType(_hydrogenTanks, (block) => block.CubeGrid == Me.CubeGrid);
                GridTerminalSystem.GetBlocksOfType(_batteries, (block) => block.CubeGrid == Me.CubeGrid);

                // Initialize core systems
                _flightController = new STUFlightController(GridTerminalSystem, _remoteControl, Me);
                s_telemetryBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_TELEMETRY_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
                s_logBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_LOG_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
                _logScreen = new LogLCD(GridTerminalSystem.GetBlockWithName("BC9 LogLCD"), 0, "Monospace", 0.5f);
                _droneListener = IGC.UnicastListener;

                // Initialize inventory enumerator; we only want drills and cargo blocks when considering filled ratio
                GridTerminalSystem.GetBlocksOfType(_droneCargoContainers, (block) => block.CubeGrid == Me.CubeGrid);

                List<IMyTerminalBlock> inventoryBlocks = new List<IMyTerminalBlock>();
                inventoryBlocks.AddRange(_droneCargoContainers);
                inventoryBlocks.AddRange(_drills);

                _inventoryEnumerator = new STUInventoryEnumerator(GridTerminalSystem, inventoryBlocks, Me);
                _damageMonitor = new STUDamageMonitor(GridTerminalSystem, Me);

                // Set runtime frequency and initalize drone state
                MinerMainState = MinerState.INITIALIZE;
                s_droneData = new MiningDroneData();
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            } catch (Exception e) {
                Echo(e.StackTrace);
                MinerMainState = MinerState.ERROR;
            } finally {
                WriteAllLogs();
            }
        }

        /// <summary>
        /// Parses CustomData string for miner configuration.
        /// </summary>
        /// <param name="configurationString"></param>
        /// <exception cref="Exception"></exception>
        void ParseMinerConfiguration(string configurationString) {
            MyIniParseResult result;
            if (!_ini.TryParse(configurationString, out result)) {
                throw new Exception("Issue parsing configuration in Custom Data");
            }
            _minerName = _ini.Get("MinerConfiguration", "MinerName").ToString("DEFAULT");
            _cruiseVelocity = _ini.Get("MinerConfiguration", "CruiseVelocity").ToDouble(15);
            _cruiseAltitude = _ini.Get("MinerConfiguration", "CruiseAltitude").ToDouble(100);
            _ascendVelocity = _ini.Get("MinerConfiguration", "AscendVelocity").ToDouble(5);
            _descendVelocity = _ini.Get("MinerConfiguration", "DescendVelocity").ToDouble(-5);
            _debug = _ini.Get("MinerConfiguration", "Debug").ToBoolean(false);
        }

        public void Main() {

            try {

                _inventoryEnumerator.EnumerateInventories();
                _damageMonitor.MonitorDamage();

                // Check for insufficient parachutes
                if (!ISSUED_PARACHUTE_WARNING && _tempInventoryEnumeratorDictionary.ContainsKey("Canvas") && _tempInventoryEnumeratorDictionary["Canvas"] < _parachutes.Count) {
                    CreateWarningBroadcast("Insufficient parachutes for emergency landing! Load miner before dispatch.");
                    ISSUED_PARACHUTE_WARNING = true;
                }

                // If we're damaged, we need to land immediately
                if (_damageMonitor.DamagedBlocks.Count > 0 && !EXECUTED_EMERGENCY_LANDING) {
                    MinerMainState = MinerState.EMERGENCY_LANDING;
                    EXECUTED_EMERGENCY_LANDING = true;
                }

                if (MinerMainState != MinerState.IDLE) {
                    _flightController.UpdateState();
                }

                if (_droneListener.HasPendingMessage) {
                    try {
                        MyIGCMessage message = _droneListener.AcceptMessage();
                        STULog log = STULog.Deserialize(message.Data.ToString());
                        CreateOkBroadcast($"Received message: {log.Message}");
                        ParseCommand(log);
                    } catch (Exception e) {
                        CreateErrorBroadcast(e.Message);
                    }
                }

                switch (MinerMainState) {

                    case MinerState.INITIALIZE:
                        InitializeMiner();
                        MinerMainState = MinerState.IDLE;
                        CreateOkBroadcast("Miner initialized");
                        break;

                    case MinerState.IDLE:
                        break;

                    case MinerState.FLY_TO_JOB_SITE:
                        _droneConnector.Disconnect();
                        bool navigated = _flightController.NavigateOverPlanetSurfaceManeuver.ExecuteStateMachine();
                        if (navigated) {
                            CreateInfoBroadcast("Arrived at job site; starting drill routine");
                            if (_drillRoutineStateMachine == null || _drillRoutineStateMachine.FinishedLastJob) {
                                CreateInfoBroadcast("Starting new drill routine");
                                _drillRoutineStateMachine = new DrillRoutine(
                                    _flightController,
                                    _drills,
                                    s_droneData.JobSite,
                                    s_droneData.JobPlane,
                                    s_droneData.JobDepth,
                                    _inventoryEnumerator);
                            } else {
                                CreateInfoBroadcast("Resuming previous drill routine");
                                int lastSilo = _drillRoutineStateMachine.CurrentSilo;
                                _drillRoutineStateMachine = new DrillRoutine(
                                    _flightController,
                                    _drills,
                                    s_droneData.JobSite,
                                    s_droneData.JobPlane,
                                    s_droneData.JobDepth,
                                    _inventoryEnumerator);
                                // If the last job didn't finish, we need to reset the current silo
                                _drillRoutineStateMachine.CurrentSilo = lastSilo;
                            }
                            MinerMainState = MinerState.MINING;
                        }
                        break;

                    case MinerState.MINING:
                        if (_drillRoutineStateMachine.ExecuteStateMachine()) {
                            _flightController.NavigateOverPlanetSurfaceManeuver = new STUFlightController.NavigateOverPlanetSurface(
                                _flightController,
                                _homeBaseConnector.GetPosition() + _homeBaseConnector.WorldMatrix.Forward * _cruiseAltitude,
                                _cruiseAltitude,
                                _cruiseVelocity,
                                _ascendVelocity,
                                _descendVelocity);
                            MinerMainState = MinerState.FLY_TO_HOME_BASE;
                        }
                        break;

                    case MinerState.FLY_TO_HOME_BASE:
                        navigated = _flightController.NavigateOverPlanetSurfaceManeuver.ExecuteStateMachine();
                        if (navigated) {
                            MinerMainState = MinerState.ALIGN_WITH_BASE_CONNECTOR;
                        }
                        break;

                    case MinerState.ALIGN_WITH_BASE_CONNECTOR:
                        bool stable = _flightController.SetStableForwardVelocity(0);
                        bool aligned = _flightController.AlignShipToTarget(_homeBaseConnector.GetPosition(), _droneConnector);
                        if (aligned) {
                            // Large-grid connectors are 2.5m long, so we need to move the drone forward by 1.0m to dock
                            // Subtracted 0.25m due to error tolerance of GotoAndStop maneuver
                            Vector3D homeBaseConnectorTip = _homeBaseConnector.GetPosition() + _homeBaseConnector.WorldMatrix.Forward * 1.0;
                            // GotoAndStop maneuver automatically adjusts the reference point by 1.25m when the reference is a connector
                            _flightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(_flightController, homeBaseConnectorTip, 2, _droneConnector);
                            MinerMainState = MinerState.DOCKING;
                        }
                        break;

                    case MinerState.DOCKING:
                        _flightController.GotoAndStopManeuver.CruiseVelocity = Vector3D.Distance(_flightController.CurrentPosition, _homeBaseConnector.GetPosition()) < 50 ? 2 : _descendVelocity;
                        _flightController.GotoAndStopManeuver.ExecuteStateMachine();
                        _droneConnector.Connect();
                        // Keep aligning until we're _cruiseAltitude / 2 meters from the home base connector
                        if (Vector3D.Distance(_droneConnector.GetPosition(), _homeBaseConnector.GetPosition()) > 20) {
                            _flightController.AlignShipToTarget(_homeBaseConnector.GetPosition(), _droneConnector);
                        }
                        if (_droneConnector.Status == MyShipConnectorStatus.Connected) {
                            MinerMainState = MinerState.REFUELING;
                            // Start refueling
                            ToggleCriticalFlightSystems(false, "A");
                        } else {
                            // Do nothing
                        }
                        break;

                    case MinerState.REFUELING:
                        // Redundancy to ensure connector connects
                        _droneConnector.Connect();
                        bool refueled = _hydrogenTanks.TrueForAll(tank => tank.FilledRatio >= 0.98);
                        bool recharged = _batteries.TrueForAll(battery => battery.CurrentStoredPower == battery.MaxStoredPower);
                        if (refueled && recharged) {
                            MinerMainState = MinerState.DEPOSIT_ORES;
                        }
                        break;

                    case MinerState.DEPOSIT_ORES:
                        // Deposit dronecargocontainer ores and drill ores to home base cargo containeres
                        UpdateBaseCargoContainers();
                        // Update flight configurations to allow on-the-fly adjustments between trip
                        ParseMinerConfiguration(Me.CustomData);
                        List<IMyInventory> cargoInventories = _droneCargoContainers.ConvertAll(container => container.GetInventory());
                        List<IMyInventory> drillInventories = _drills.ConvertAll(drill => drill.GetInventory());
                        List<IMyInventory> baseInventories = _baseCargoContainers.ConvertAll(container => container.GetInventory());
                        bool depositedCargo = DepositOres(cargoInventories, baseInventories);
                        bool depositedDrills = DepositOres(drillInventories, baseInventories);
                        if (!depositedCargo || !depositedDrills) {
                            // TODO: Come up with more graceful handling of full inventories
                            throw new Exception("Could not deposit all material!");
                        }
                        if (_drillRoutineStateMachine.FinishedLastJob) {
                            // Turn everything off; we're going into IDLE
                            ToggleCriticalFlightSystems(false, "B");
                            MinerMainState = MinerState.IDLE;
                        } else {
                            // Turn everything on because we're going to fly back to the job site
                            _flightController.NavigateOverPlanetSurfaceManeuver = new STUFlightController.NavigateOverPlanetSurface(
                                _flightController,
                                GetPointAboveJobSite(_cruiseAltitude),
                                _cruiseAltitude,
                                _cruiseVelocity,
                                _ascendVelocity,
                                _descendVelocity);
                            ToggleCriticalFlightSystems(true, "C");
                            MinerMainState = MinerState.FLY_TO_JOB_SITE;
                        }
                        break;

                    case MinerState.EMERGENCY_LANDING:
                        // deploy parachutes
                        _parachutes.ForEach(parachute => parachute.Enabled = true);
                        _parachutes.ForEach(parachute => parachute.OpenDoor());

                        // turn off thrusters
                        _flightController.ToggleThrusters(false);

                        // turn on emergency beacon
                        _emergencyBeacon.Enabled = true;
                        MinerMainState = MinerState.ERROR;
                        break;

                    case MinerState.ERROR:
                        // Do nothing
                        break;

                }

            } catch (Exception e) {
                string stackTrace = e.StackTrace.Replace("\n", " ");
                CreateFatalErrorBroadcast(stackTrace);
                _flightController.RelinquishGyroControl();
                _flightController.RelinquishThrusterControl();
                _flightController.ToggleDampeners(true);
                ToggleCriticalFlightSystems(true, "D");
                // Remove all override, allow dampeners to stabilize vehicle
                for (int i = 0; i < _flightController.ActiveThrusters.Length; i++) {
                    _flightController.ActiveThrusters[i].ThrustOverride = 0;
                }
                MinerMainState = MinerState.ERROR;
            } finally {
                WriteAllLogs();
                UpdateTelemetry();
            }
        }

        void WriteAllLogs() {
            // Insert FlightController diagnostic logs for local display if debug mode is on
            if (_debug) {
                GetFlightControllerLogs();
            }
            _logScreen.StartFrame();
            _logScreen.WriteWrappableLogs(_logScreen.FlightLogs);
            _logScreen.EndAndPaintFrame();
        }

        void GetFlightControllerLogs() {
            while (STUFlightController.FlightLogs.Count > 0) {
                _tempFlightLog = STUFlightController.FlightLogs.Dequeue();
                _tempFlightLog.Sender = $"{s_droneData.Name} (FC)";
                s_logBroadcaster.Log(_tempFlightLog);
                _logScreen.FlightLogs.Enqueue(_tempFlightLog);
            }
        }

        void ToggleCriticalFlightSystems(bool on, string location) {
            if (_debug) {
                string output = $"Toggle critical systems: {on}; {location}";
                CreateInfoBroadcast(output);
                Echo(output);
            }
            _batteries.ForEach(battery => battery.ChargeMode = on ? ChargeMode.Auto : ChargeMode.Recharge);
            _hydrogenTanks.ForEach(tank => tank.Stockpile = !on);
            _flightController.ToggleThrusters(on);
        }

        void ParseCommand(STULog log) {

            string command = log.Message;
            Dictionary<string, string> metadata = log.Metadata;

            if (string.IsNullOrEmpty(command)) {
                CreateErrorBroadcast("Command is empty");
                return;
            }

            switch (command) {

                case "SetJobSite":
                    SetJobSite(metadata);
                    break;

                case "Stop":
                    if (Stop()) {
                        CreateOkBroadcast("Miner stopped");
                    } else {
                        CreateErrorBroadcast("Error stopping miner");
                    }
                    break;

                default:
                    CreateErrorBroadcast($"Unknown command: {command}");
                    break;

            }

        }

        void SetJobSite(Dictionary<string, string> metadata) {
            s_droneData.JobSite = MiningDroneData.DeserializeVector3D(metadata["JobSite"]);
            s_droneData.JobPlane = MiningDroneData.DeserializePlaneD(metadata["JobPlane"]);
            int depth;
            if (!int.TryParse(metadata["JobDepth"], out depth)) {
                throw new Exception("Error parsing job depth!");
            }
            s_droneData.JobDepth = depth;
            _droneConnector.Disconnect();
            ToggleCriticalFlightSystems(true, "E");
            // Calculate the cruise phase destination
            _flightController.NavigateOverPlanetSurfaceManeuver = new STUFlightController.NavigateOverPlanetSurface(
                _flightController,
                GetPointAboveJobSite(_cruiseAltitude),
                _cruiseAltitude,
                _cruiseVelocity,
                _ascendVelocity,
                _descendVelocity);
            MinerMainState = MinerState.FLY_TO_JOB_SITE;
        }

        Vector3D GetPointAboveJobSite(double altitude) {
            STUGalacticMap.Planet? currentPlanet = STUGalacticMap.GetPlanetOfPoint(_flightController.CurrentPosition, 10000);
            if (currentPlanet == null) {
                throw new Exception($"Cannot determine planet! Sea-level altitude: {_flightController.GetCurrentSeaLevelAltitude()}");
            }
            Vector3D jobSiteVector = s_droneData.JobSite - currentPlanet.Value.Center;
            jobSiteVector.Normalize();
            Vector3D destination = currentPlanet.Value.Center + jobSiteVector * (Vector3D.Distance(currentPlanet.Value.Center, s_droneData.JobSite) + altitude);
            return destination;
        }

        bool DepositOres(List<IMyInventory> inputInventories, List<IMyInventory> outputInventories) {

            foreach (IMyInventory inputInventory in inputInventories) {
                _tempInventoryItems.Clear();
                inputInventory.GetItems(_tempInventoryItems);

                foreach (MyInventoryItem item in _tempInventoryItems) {
                    MyItemType type = item.Type;
                    bool itemTransferred = false;

                    foreach (IMyInventory outputInventory in outputInventories) {
                        bool canTransfer = inputInventory.CanTransferItemTo(outputInventory, type);
                        bool enoughSpace = outputInventory.CanItemsBeAdded(item.Amount, type);

                        if (canTransfer && enoughSpace) {
                            bool success = inputInventory.TransferItemTo(outputInventory, item, item.Amount);
                            if (success) {
                                itemTransferred = true;
                                break; // Item transferred, move to the next item
                            }
                        }
                    }

                    // If we can't transfer this item to any inventory, there's really no point in depositing the rest of the items
                    // We want the drone to be completely empty before taking off again
                    if (!itemTransferred) {
                        return false;
                    }

                }
            }

            return true;
        }

        bool Stop() {
            // todo
            return true;
        }

        void InitializeMiner() {
            if (!_droneConnector.IsConnected) {
                throw new Exception("Miner not connected to base; exiting");
            }
            _homeBaseConnector = _droneConnector.OtherConnector;
            UpdateBaseCargoContainers();
            ToggleCriticalFlightSystems(false, "F");
        }

        void UpdateBaseCargoContainers() {
            if (_homeBaseConnector == null) {
                throw new Exception("Need home base connector reference to find base cargo containers");
            }
            GridTerminalSystem.GetBlocksOfType(_baseCargoContainers, (block) => block.CubeGrid == _homeBaseConnector.CubeGrid);
        }

        void UpdateTelemetry() {
            // Update drone state
            _tempInventoryEnumeratorDictionary = _inventoryEnumerator.GetItemTotals();
            s_droneData.State = MinerMainState;
            s_droneData.WorldPosition = _flightController.CurrentPosition;
            s_droneData.WorldVelocity = _flightController.CurrentVelocity_WorldFrame;
            s_droneData.PowerMegawatts = _tempInventoryEnumeratorDictionary.ContainsKey("Power") ? _tempInventoryEnumeratorDictionary["Power"] : 0;
            s_droneData.HydrogenLiters = _tempInventoryEnumeratorDictionary.ContainsKey("Hydrogen") ? _tempInventoryEnumeratorDictionary["Hydrogen"] : 0;
            s_droneData.PowerCapacity = _inventoryEnumerator.PowerCapacity;
            s_droneData.HydrogenCapacity = _inventoryEnumerator.HydrogenCapacity;
            s_droneData.Name = _minerName;
            s_droneData.Id = _minerId;
            s_droneData.CargoLevel = _inventoryEnumerator.FilledRatio;
            s_droneData.CargoCapacity = _inventoryEnumerator.StorageCapacity;

            _tempMetadataDictionary["MinerDroneData"] = s_droneData.Serialize();
            _tempOutgoingLog.Message = "";
            _tempOutgoingLog.Type = STULogType.INFO;
            _tempOutgoingLog.Sender = _minerName;
            _tempOutgoingLog.Metadata = _tempMetadataDictionary;

            // Send data to HQ
            s_telemetryBroadcaster.Log(_tempOutgoingLog);

            // Debug output
            Echo(_tempMetadataDictionary["MinerDroneData"]);

        }

        List<IMyLightingBlock> GetStatusLights() {
            List<IMyLightingBlock> statusLights = new List<IMyLightingBlock>();
            GridTerminalSystem.GetBlocksOfType(statusLights, light =>
                light.CubeGrid == Me.CubeGrid &&
                MyIni.HasSection(light.CustomData, "MinerStatusLight")
            );
            statusLights.ForEach(light => {
                light.Enabled = true;
                light.Radius = 3.0f;
                light.Falloff = 0.5f;
                light.Intensity = 5.0f;
                light.BlinkOffset = 0.0f;
            });
            return statusLights;
        }

        IMyRemoteControl GetRemoteControl() {
            List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remoteControls, block =>
                block.CubeGrid == Me.CubeGrid &&
                MyIni.HasSection(block.CustomData, "MinerRemoteControl")
            );
            if (remoteControls.Count == 0) {
                throw new Exception("No remote control found");
            }
            if (remoteControls.Count > 1) {
                throw new Exception("Multiple remote controls found");
            }
            return remoteControls[0];
        }

        IMyShipConnector GetConnector() {
            List<IMyShipConnector> connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType(connectors, block =>
                block.CubeGrid == Me.CubeGrid &&
                MyIni.HasSection(block.CustomData, "MinerConnector")
            );
            if (connectors.Count == 0) {
                throw new Exception("No ship connector found");
            }
            if (connectors.Count > 1) {
                throw new Exception("Multiple ship connectors found");
            }
            return connectors[0];
        }

        IMyBeacon GetEmergencyBeacon() {
            IMyBeacon beacon = GridTerminalSystem.GetBlockWithName("Emergency Beacon") as IMyBeacon;
            if (beacon == null) {
                throw new Exception("No emergency beacon found");
            }
            return beacon;
        }

        List<IMyParachute> GetParachutes() {
            List<IMyParachute> parachutes = new List<IMyParachute>();
            GridTerminalSystem.GetBlocksOfType(parachutes, block => block.CubeGrid == Me.CubeGrid && MyIni.HasSection(block.CustomData, "MinerParachute"));
            // if no parachutes, error
            if (parachutes.Count == 0) {
                throw new Exception("No parachutes found");
            }
            return parachutes;
        }

        void SetStatusLights(string status) {
            foreach (var light in _statusLights) {
                switch (status) {
                    case MinerState.IDLE:
                        // solid green light
                        light.Color = Color.Green;
                        light.BlinkIntervalSeconds = 0;
                        light.BlinkLength = 0;
                        break;
                    case MinerState.ERROR:
                        // slow blinking red light
                        light.Color = Color.Red;
                        light.BlinkIntervalSeconds = 2.0f;
                        light.BlinkLength = 50.0f;
                        break;
                    default:
                        // quick blinking yellow
                        light.Color = Color.Yellow;
                        light.BlinkIntervalSeconds = 0.5f;
                        light.BlinkLength = 25.0f;
                        break;
                }

            }
        }

        // Broadcast utilities
        #region
        static void CreateBroadcast(string message, string type) {
            s_logBroadcaster.Log(new STULog() {
                Message = message,
                Type = type,
                Sender = _minerName,
            });
            _logScreen.FlightLogs.Enqueue(new STULog() {
                Message = message,
                Type = type,
                Sender = _minerName,
            });
        }

        static void CreateOkBroadcast(string message) {
            CreateBroadcast(message, STULogType.OK);
        }

        static void CreateWarningBroadcast(string message) {
            CreateBroadcast(message, STULogType.WARNING);
        }

        static void CreateErrorBroadcast(string message) {
            CreateBroadcast(message, STULogType.ERROR);
        }

        static void CreateInfoBroadcast(string message) {
            CreateBroadcast(message, STULogType.INFO);
        }

        void CreateFatalErrorBroadcast(string message) {
            CreateBroadcast($"FATAL - {message}. Script has stopped running.", STULogType.ERROR);
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }
        #endregion

    }

}
