using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {

    partial class Program : MyGridProgram {

        MyIni _ini = new MyIni();

        static string _minerName;
        int _cruiseAltitude;
        int _cruiseVelocity;
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

        string MinerMainState;
        string _minerMainState {
            get { return _minerMainState; }
            set {
                _minerMainState = value;
                CreateInfoBroadcast($"Transitioning to {value}");
                _flightController.UpdateShipMass();
            }
        }

        List<IMyShipDrill> _drills = new List<IMyShipDrill>();
        List<IMyGasTank> _hydrogenTanks = new List<IMyGasTank>();
        List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();

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

        public Program() {

            ParseMinerConfiguration(Me.CustomData);
            _minerId = Me.EntityId.ToString();

            // Get blocks needed for various systems
            _remoteControl = GridTerminalSystem.GetBlockWithName("FC Remote Control") as IMyRemoteControl;
            _droneConnector = GridTerminalSystem.GetBlockWithName("Main Connector") as IMyShipConnector;

            GridTerminalSystem.GetBlocksOfType(_drills, (block) => block.CubeGrid == Me.CubeGrid);
            GridTerminalSystem.GetBlocksOfType(_hydrogenTanks, (block) => block.CubeGrid == Me.CubeGrid);
            GridTerminalSystem.GetBlocksOfType(_batteries, (block) => block.CubeGrid == Me.CubeGrid);

            // Initialize core systems
            _flightController = new STUFlightController(GridTerminalSystem, _remoteControl, Me);
            s_telemetryBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_TELEMETRY_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            s_logBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_LOG_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            _logScreen = new LogLCD(GridTerminalSystem.GetBlockWithName("LogLCD"), 0, "Monospace", 0.5f);
            _droneListener = IGC.UnicastListener;

            // Initialize inventory enumerator; we only want drills and cargo blocks when considering filled ratio
            GridTerminalSystem.GetBlocksOfType(_droneCargoContainers, (block) => block.CubeGrid == Me.CubeGrid);

            List<IMyTerminalBlock> inventoryBlocks = new List<IMyTerminalBlock>();
            inventoryBlocks.AddRange(_droneCargoContainers);
            inventoryBlocks.AddRange(_drills);

            _inventoryEnumerator = new STUInventoryEnumerator(GridTerminalSystem, inventoryBlocks, Me);

            // Set runtime frequency and initalize drone state
            MinerMainState = MinerState.INITIALIZE;
            s_droneData = new MiningDroneData();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

        }

        void ParseMinerConfiguration(string configurationString) {
            MyIniParseResult result;
            if (!_ini.TryParse(configurationString, out result)) {
                throw new Exception("Issue parsing configuration in Custom Data");
            }
            _minerName = _ini.Get("MinerConfiguration", "MinerName").ToString("DEFAULT");
            _cruiseVelocity = _ini.Get("MinerConfiguration", "CruiseVelocity").ToInt32(15);
            _cruiseAltitude = _ini.Get("MinerConfiguration", "CruiseAltitude").ToInt32(100);
            _debug = _ini.Get("MinerConfiguration", "Debug").ToBoolean(false);
        }

        public void Main() {

            // Coroutine to update item and fuel inventory counts
            _inventoryEnumerator.EnumerateInventories();

            try {

                UpdateTelemetry();
                _inventoryEnumerator.EnumerateInventories();
                _flightController.UpdateState();

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
                        if (_droneConnector.IsConnected) {
                            _droneConnector.Disconnect();
                        }
                        bool navigated = _flightController.NavigateOverPlanetSurfaceManeuver.ExecuteStateMachine();
                        if (navigated) {
                            CreateInfoBroadcast("Arrived at job site; starting drill routine");
                            if (_drillRoutineStateMachine == null || _drillRoutineStateMachine.FinishedLastJob) {
                                CreateInfoBroadcast("Starting new drill routine");
                                _drillRoutineStateMachine = new DrillRoutine(_flightController, _drills, s_droneData.JobSite, s_droneData.JobPlane, s_droneData.JobDepth, _inventoryEnumerator);
                            } else {
                                CreateInfoBroadcast("Resuming previous drill routine");
                                int lastSilo = _drillRoutineStateMachine.CurrentSilo;
                                _drillRoutineStateMachine = new DrillRoutine(_flightController, _drills, s_droneData.JobSite, s_droneData.JobPlane, s_droneData.JobDepth, _inventoryEnumerator);
                                // If the last job didn't finish, we need to reset the current silo
                                _drillRoutineStateMachine.CurrentSilo = lastSilo;
                            }
                            MinerMainState = MinerState.MINING;
                        }
                        break;

                    case MinerState.MINING:
                        if (_drillRoutineStateMachine.ExecuteStateMachine()) {
                            _flightController.NavigateOverPlanetSurfaceManeuver = new STUFlightController.NavigateOverPlanetSurface(_flightController, _homeBaseConnector.GetPosition() + _homeBaseConnector.WorldMatrix.Forward * 20, _cruiseAltitude, _cruiseVelocity);
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
                        if (stable && aligned) {
                            // Large-grid connectors are 2.5m long, so we need to move the drone forward by 1.0m to dock
                            // Subtracted 0.25m due to error tolerance of GotoAndStop maneuver
                            Vector3D homeBaseConnectorTip = _homeBaseConnector.GetPosition() + _homeBaseConnector.WorldMatrix.Forward * 1.0;
                            // GotoAndStop maneuver automatically adjusts the reference point by 1.25m when the reference is a connector
                            _flightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(_flightController, homeBaseConnectorTip, 1, _droneConnector);
                            MinerMainState = MinerState.DOCKING;
                        }
                        break;

                    case MinerState.DOCKING:
                        _flightController.GotoAndStopManeuver.ExecuteStateMachine();
                        _droneConnector.Connect();
                        if (_droneConnector.Status == MyShipConnectorStatus.Connected) {
                            MinerMainState = MinerState.REFUELING;
                            // Start refueling
                            _hydrogenTanks.ForEach(tank => tank.Stockpile = true);
                            _batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Recharge);
                            _flightController.ToggleThrusters(false);
                        } else {
                        }
                        break;

                    case MinerState.REFUELING:
                        bool refueled = _hydrogenTanks.TrueForAll(tank => tank.FilledRatio == 1);
                        bool recharged = _batteries.TrueForAll(battery => battery.CurrentStoredPower == battery.MaxStoredPower);
                        if (refueled && recharged) {
                            MinerMainState = MinerState.DEPOSIT_ORES;
                            // Turn these back on
                            _hydrogenTanks.ForEach(tank => tank.Stockpile = false);
                            _batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Auto);
                            _flightController.ToggleThrusters(true);
                        }
                        break;

                    case MinerState.DEPOSIT_ORES:
                        // Deposit dronecargocontainer ores and drill ores to home base cargo containeres
                        UpdateBaseCargoContainers();
                        List<IMyInventory> cargoInventories = _droneCargoContainers.ConvertAll(container => container.GetInventory());
                        List<IMyInventory> drillInventories = _drills.ConvertAll(drill => drill.GetInventory());
                        List<IMyInventory> baseInventories = _baseCargoContainers.ConvertAll(container => container.GetInventory());
                        bool depositedCargo = DepositOres(cargoInventories, baseInventories);
                        bool depositedDrills = DepositOres(drillInventories, baseInventories);
                        if (!depositedCargo || !depositedDrills) {
                            // TODO: Come up with more graceful handling of full inventories
                            CreateFatalErrorBroadcast("Could not deposit all material!");
                        }
                        if (_drillRoutineStateMachine.FinishedLastJob) {
                            MinerMainState = MinerState.IDLE;
                        } else {
                            _flightController.NavigateOverPlanetSurfaceManeuver = new STUFlightController.NavigateOverPlanetSurface(_flightController, GetPointAboveJobSite(50), _cruiseAltitude, _cruiseVelocity);
                            MinerMainState = MinerState.FLY_TO_JOB_SITE;
                        }
                        break;

                }

            } catch (Exception e) {
                CreateFatalErrorBroadcast(e.StackTrace);
                _flightController.RelinquishGyroControl();
                _flightController.RelinquishThrusterControl();
                _hydrogenTanks.ForEach(tank => tank.Stockpile = false);
                _batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Auto);
                _flightController.ToggleDampeners(true);
            } finally {
                // Insert FlightController diagnostic logs for local display if debug mode is on
                if (_debug) {
                    GetFlightControllerLogs();
                }
                _logScreen.StartFrame();
                _logScreen.WriteWrappableLogs(_logScreen.FlightLogs);
                _logScreen.EndAndPaintFrame();
            }
        }

        void GetFlightControllerLogs() {
            while (STUFlightController.FlightLogs.Count > 0) {
                _tempFlightLog = STUFlightController.FlightLogs.Dequeue();
                s_logBroadcaster.Log(_tempFlightLog);
                _logScreen.FlightLogs.Enqueue(_tempFlightLog);
            }
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
            _hydrogenTanks.ForEach(tank => tank.Stockpile = false);
            _batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Auto);

            // Calculate the cruise phase destination
            _flightController.NavigateOverPlanetSurfaceManeuver = new STUFlightController.NavigateOverPlanetSurface(_flightController, GetPointAboveJobSite(50), _cruiseAltitude, _cruiseVelocity);
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
                CreateFatalErrorBroadcast("Miner not connected to base; exiting");
            }
            _flightController.ToggleThrusters(true);
            _homeBaseConnector = _droneConnector.OtherConnector;
            UpdateBaseCargoContainers();
            _hydrogenTanks.ForEach(tank => tank.Stockpile = true);
            _batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Recharge);
        }

        void UpdateBaseCargoContainers() {
            if (_homeBaseConnector == null) {
                CreateFatalErrorBroadcast("Need home base connector reference to find base cargo containers");
            }
            GridTerminalSystem.GetBlocksOfType(_baseCargoContainers, (block) => block.CubeGrid == _homeBaseConnector.CubeGrid);
        }

        void UpdateTelemetry() {
            // Update drone state
            _tempInventoryEnumeratorDictionary = _inventoryEnumerator.GetItemTotals();
            s_droneData.State = MinerMainState;
            s_droneData.WorldPosition = _flightController.CurrentPosition;
            s_droneData.WorldVelocity = _flightController.CurrentVelocity_WorldFrame;
            s_droneData.BatteryLevel = _tempInventoryEnumeratorDictionary.ContainsKey("Battery") ? _tempInventoryEnumeratorDictionary["Battery"] : 0;
            s_droneData.HydrogenLevel = _tempInventoryEnumeratorDictionary.ContainsKey("Hydrogen") ? _tempInventoryEnumeratorDictionary["Hydrogen"] : 0;
            s_droneData.Name = _minerName;
            s_droneData.Id = _minerId;
            s_droneData.CargoLevel = _inventoryEnumerator.FilledRatio;
            s_droneData.CargoCapacity = 0;

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
