using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {

    partial class Program : MyGridProgram {

        static string MinerName;

        IMyUnicastListener DroneListener;

        static STUMasterLogBroadcaster TelemetryBroadcaster { get; set; }
        static STUMasterLogBroadcaster LogBroadcaster { get; set; }
        STUFlightController FlightController { get; set; }
        STURaycaster Raycaster { get; set; }
        IMyRemoteControl RemoteControl { get; set; }

        IMyShipConnector DroneConnector { get; set; }
        IMyShipConnector HomeBaseConnector { get; set; }

        string MinerMainState { get; set; }

        List<IMyShipDrill> Drills = new List<IMyShipDrill>();
        List<IMyGasTank> HydrogenTanks = new List<IMyGasTank>();
        List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();

        Queue<STULog> FlightLogs { get; set; }

        static LogLCD LogScreen { get; set; }

        // Subroutine declarations
        DrillRoutine DrillRoutineStateMachine { get; set; }

        // Inventory enumerator
        STUInventoryEnumerator InventoryEnumerator { get; set; }

        static MiningDroneData DroneData { get; set; }

        public string MinerId { get; protected set; }

        public Program() {

            // Ensure miner has a name
            MinerName = Me.CustomData;
            MinerId = Me.EntityId.ToString();
            if (MinerName.Length == 0) {
                CreateFatalErrorBroadcast("Miner name not set in custom data");
            }

            // Get blocks needed for various systems
            RemoteControl = GridTerminalSystem.GetBlockWithName("FC Remote Control") as IMyRemoteControl;
            DroneConnector = GridTerminalSystem.GetBlockWithName("Main Connector") as IMyShipConnector;

            GridTerminalSystem.GetBlocksOfType(Drills, (block) => block.CubeGrid == Me.CubeGrid);
            GridTerminalSystem.GetBlocksOfType(HydrogenTanks, (block) => block.CubeGrid == Me.CubeGrid);
            GridTerminalSystem.GetBlocksOfType(Batteries, (block) => block.CubeGrid == Me.CubeGrid);

            // Initialize core systems
            FlightController = new STUFlightController(GridTerminalSystem, RemoteControl, Me);
            TelemetryBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_TELEMETRY_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            LogBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_LOG_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            LogScreen = new LogLCD(Me, 0, "Monospace", 0.5f);
            DroneListener = IGC.UnicastListener;

            // Initialize inventory enumerator; we only want drills and cargo blocks when considering filled ratio
            List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType(cargoContainers);
            List<IMyTerminalBlock> inventoryBlocks = new List<IMyTerminalBlock>();
            inventoryBlocks.AddRange(cargoContainers);
            inventoryBlocks.AddRange(Drills);
            InventoryEnumerator = new STUInventoryEnumerator(GridTerminalSystem, inventoryBlocks, Me);


            // Set runtime frequency and initalize drone state
            MinerMainState = MinerState.INITIALIZE;
            DroneData = new MiningDroneData();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

        }

        public void Main() {

            // Coroutine to update item and fuel inventory counts
            InventoryEnumerator.EnumerateInventories();

            try {

                UpdateTelemetry();
                InventoryEnumerator.EnumerateInventories();
                FlightController.UpdateState();

                if (DroneListener.HasPendingMessage) {
                    try {
                        MyIGCMessage message = DroneListener.AcceptMessage();
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
                        bool navigated = FlightController.NavigateOverPlanetSurfaceManeuver.ExecuteStateMachine();
                        if (navigated) {
                            CreateInfoBroadcast("Arrived at job site; starting drill routine");
                            if (DrillRoutineStateMachine == null || DrillRoutineStateMachine.FinishedLastJob) {
                                CreateInfoBroadcast("Starting new drill routine");
                                DrillRoutineStateMachine = new DrillRoutine(FlightController, Drills, DroneData.JobSite, DroneData.JobPlane, InventoryEnumerator);
                            } else {
                                CreateInfoBroadcast("Resuming previous drill routine");
                                int lastSilo = DrillRoutineStateMachine.CurrentSilo;
                                DrillRoutineStateMachine = new DrillRoutine(FlightController, Drills, DroneData.JobSite, DroneData.JobPlane, InventoryEnumerator);
                                // If the last job didn't finish, we need to reset the current silo
                                DrillRoutineStateMachine.CurrentSilo = lastSilo;
                            }
                            MinerMainState = MinerState.MINING;
                        }
                        break;

                    case MinerState.MINING:
                        if (DrillRoutineStateMachine.ExecuteStateMachine()) {
                            CreateInfoBroadcast("Returning to base");
                            FlightController.NavigateOverPlanetSurfaceManeuver = new STUFlightController.NavigateOverPlanetSurface(FlightController, HomeBaseConnector.GetPosition() + HomeBaseConnector.WorldMatrix.Forward * 20, 30, 5);
                            MinerMainState = MinerState.FLY_TO_HOME_BASE;
                        }
                        break;

                    case MinerState.FLY_TO_HOME_BASE:
                        navigated = FlightController.NavigateOverPlanetSurfaceManeuver.ExecuteStateMachine();
                        if (navigated) {
                            MinerMainState = MinerState.ALIGN_WITH_BASE_CONNECTOR;
                        }
                        break;

                    case MinerState.ALIGN_WITH_BASE_CONNECTOR:
                        bool stable = FlightController.SetStableForwardVelocity(0);
                        bool aligned = FlightController.AlignShipToTarget(HomeBaseConnector.GetPosition(), DroneConnector);
                        if (stable && aligned) {
                            CreateInfoBroadcast("Aligned with base connector; connecting");
                            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, HomeBaseConnector.GetPosition(), 1, DroneConnector);
                            MinerMainState = MinerState.DOCKING;
                        }
                        break;

                    case MinerState.DOCKING:
                        navigated = FlightController.GotoAndStopManeuver.ExecuteStateMachine();
                        if (navigated) {
                            DroneConnector.Connect();
                            if (DroneConnector.Status == MyShipConnectorStatus.Connected) {
                                CreateInfoBroadcast("Docked with base connector; refueling");
                                MinerMainState = MinerState.REFUELING;
                            } else {
                                CreateWarningBroadcast("Failed to dock; retrying");
                            }
                        }
                        break;

                }

            } catch (Exception e) {
                CreateFatalErrorBroadcast(e.Message);
            } finally {
                // Insert FlightController diagnostic logs for local display
                GetFlightControllerLogs();
                foreach (var log in STUFlightController.FlightLogs) {
                    CreateBroadcast(log.Message, log.Type);
                }
                LogScreen.StartFrame();
                LogScreen.WriteWrappableLogs(LogScreen.FlightLogs);
                LogScreen.EndAndPaintFrame();
            }
        }

        void GetFlightControllerLogs() {
            while (STUFlightController.FlightLogs.Count > 0) {
                LogScreen.FlightLogs.Enqueue(STUFlightController.FlightLogs.Dequeue());
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
            DroneData.JobSite = MiningDroneData.DeserializeVector3D(metadata["JobSite"]);
            DroneData.JobPlane = MiningDroneData.DeserializePlaneD(metadata["JobPlane"]);
            DroneConnector.Disconnect();
            HydrogenTanks.ForEach(tank => tank.Stockpile = false);
            Batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Auto);
            // Calculate the cruise phase destination
            STUGalacticMap.Planet currentPlanet = STUGalacticMap.GetPlanetOfPoint(FlightController.CurrentPosition).Value;
            Vector3D jobSiteVector = DroneData.JobSite - currentPlanet.Center;
            jobSiteVector.Normalize();
            Vector3D cruiseDestination = currentPlanet.Center + jobSiteVector * (Vector3D.Distance(currentPlanet.Center, DroneData.JobSite) + 30);
            FlightController.NavigateOverPlanetSurfaceManeuver = new STUFlightController.NavigateOverPlanetSurface(FlightController, cruiseDestination, 30, 5);
            CreateOkBroadcast("Job site set; moving to FLY_TO_JOB_SITE");
            MinerMainState = MinerState.FLY_TO_JOB_SITE;
        }

        bool Stop() {
            // todo
            return true;
        }


        void InitializeMiner() {
            if (!DroneConnector.IsConnected) {
                CreateFatalErrorBroadcast("Miner not connected to base; exiting");
            }
            HomeBaseConnector = DroneConnector.OtherConnector;
            CreateInfoBroadcast($"Home base connector pos: {HomeBaseConnector.GetPosition() + HomeBaseConnector.WorldMatrix.Forward * 50}");
            HydrogenTanks.ForEach(tank => tank.Stockpile = true);
            Batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Recharge);
        }

        void UpdateTelemetry() {
            // Update drone state
            Dictionary<string, double> inventory = InventoryEnumerator.GetItemTotals();
            DroneData.State = MinerMainState;
            DroneData.WorldPosition = FlightController.CurrentPosition;
            DroneData.WorldVelocity = FlightController.CurrentVelocity_WorldFrame;
            DroneData.BatteryLevel = inventory.ContainsKey("Battery") ? inventory["Battery"] : 0;
            DroneData.HydrogenLevel = inventory.ContainsKey("Hydrogen") ? inventory["Hydrogen"] : 0;
            DroneData.Name = MinerName;
            DroneData.Id = MinerId;
            DroneData.CargoLevel = 0;
            DroneData.CargoCapacity = 0;

            // Send data to HQ
            TelemetryBroadcaster.Log(new STULog() {
                Message = "",
                Type = STULogType.INFO,
                Sender = MinerName,
                Metadata = new Dictionary<string, string>() {
                    { "MinerDroneData", DroneData.Serialize() }
                }
            });

            // Debug output
            Echo(DroneData.Serialize());
        }

        // Broadcast utilities
        #region
        static void CreateBroadcast(string message, string type) {
            LogBroadcaster.Log(new STULog() {
                Message = message,
                Type = type,
                Sender = MinerName,
            });
            LogScreen.FlightLogs.Enqueue(new STULog() {
                Message = message,
                Type = type,
                Sender = MinerName,
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
