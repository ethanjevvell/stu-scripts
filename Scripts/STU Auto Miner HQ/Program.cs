using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyBroadcastListener DroneListener;
        IMyBroadcastListener ReconListener;

        STUMasterLogBroadcaster HQToDroneBroadcaster;

        MyIGCMessage IGCMessage;

        // MAIN LCDS
        List<IMyTerminalBlock> MainSubscribers = new List<IMyTerminalBlock>();

        // LOG LCDS
        List<LogLCD> LogSubscribers = new List<LogLCD>();

        Dictionary<string, MiningDroneData> MiningDrones = new Dictionary<string, MiningDroneData>();

        // Holds log data temporarily for each run
        STULog IncomingLog;

        Dictionary<string, MiningDroneData> IncomingDroneData = new Dictionary<string, MiningDroneData>();

        public Program() {

            // Register listeners
            DroneListener = IGC.RegisterBroadcastListener(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_CHANNEL);
            ReconListener = IGC.RegisterBroadcastListener(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_RECON_CHANNEL);

            // Initialize HQ to drone broadcaster
            HQToDroneBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_COMMAND_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            LogSubscribers = DiscoverSubscribers(GridTerminalSystem);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {

            try {
                UpdateDroneTelemetry();
                if (ReconListener.HasPendingMessage) {
                    IGCMessage = ReconListener.AcceptMessage();
                    IncomingLog = STULog.Deserialize(IGCMessage.Data.ToString());
                    PublishExternalLog(IncomingLog);
                    DispatchDrone(IncomingLog);
                }
            } catch (Exception e) {
                CreateHQLog($"Error in Main(): {e.Message}. Terminating script.", STULogType.ERROR);
                Runtime.UpdateFrequency = UpdateFrequency.None;
            } finally {
                WriteAllLogScreens();
            }

        }

        List<LogLCD> DiscoverSubscribers(IMyGridTerminalSystem grid) {
            List<LogLCD> outputList = new List<LogLCD>();
            List<IMyTerminalBlock> gridBlocks = new List<IMyTerminalBlock>();
            grid.GetBlocks(gridBlocks);
            for (int i = 0; i < gridBlocks.Count; i++) {
                if (gridBlocks[i] is IMyTextPanel) {
                    IMyTextPanel panel = gridBlocks[i] as IMyTextPanel;
                    LogLCD logLCD;
                    TryParseLogConfiguration(panel, out logLCD);
                    if (logLCD != null) {
                        logLCD.Clear();
                        outputList.Add(logLCD);
                    }
                }
            }
            return outputList;
        }

        void TryParseLogConfiguration(IMyTerminalBlock block, out LogLCD logLCD) {

            LogLCD output = null;

            float minFontSize = 0.1f;
            float maxFontSize = 10f;

            string customData = block.CustomData;
            string[] lines = customData.Split('\n');

            foreach (var line in lines) {
                if (line.Contains(AUTO_MINER_VARIABLES.AUTO_MINER_LOG_SUBSCRIBER_TAG)) {
                    string[] kvp = line.Split(':');
                    // skip lines if they don't have the right number of elements; it can't be a log configuration line in that case
                    if (kvp.Length != 3) {
                        continue;
                    }
                    float fontSize = float.Parse(kvp[2]);
                    if (fontSize < minFontSize || fontSize > maxFontSize) {
                        throw new Exception($"Invalid font size for {block.Name}; fontSize must be between {minFontSize} and {maxFontSize}");
                    }
                    output = new LogLCD(block, int.Parse(kvp[1]), "Monospace", fontSize);
                    break;
                }
            }

            logLCD = output;
        }

        /// <summary>
        /// Processes incoming drone telemetry messages to determine the state of each drone, discover new drones, and update existing drones.
        /// </summary>
        void UpdateDroneTelemetry() {

            IncomingDroneData.Clear();

            while (DroneListener.HasPendingMessage) {
                MyIGCMessage message = DroneListener.AcceptMessage();
                try {
                    IncomingLog = STULog.Deserialize(message.Data.ToString());

                    // By convention, blank message fields mean the transmission only contains telemetry data
                    if (!string.IsNullOrEmpty(IncomingLog.Message)) {
                        // If it's just a message, publish it to the log screens and move on to the next incoming message
                        PublishExternalLog(IncomingLog);
                        continue;
                    }

                    // Check if Metadata is not null and contains the key "MinerDroneData"
                    if (!IncomingLog.Metadata.ContainsKey("MinerDroneData")) {
                        CreateHQLog("Incoming log does not contain MinerDroneData", STULogType.ERROR);
                        continue; // Skip processing this message
                    }

                    // Proceed to deserialize the drone data
                    MiningDroneData drone = MiningDroneData.Deserialize(IncomingLog.Metadata["MinerDroneData"]);
                    IncomingDroneData.Add(drone.Id, drone);

                    // Update or add the drone to the MiningDrones dictionary
                    if (MiningDrones.ContainsKey(drone.Id)) {
                        if (MiningDrones[drone.Id].State == MinerState.MISSING) {
                            CreateHQLog($"Drone {drone.Id} has returned", STULogType.OK);
                        }
                        MiningDrones[drone.Id] = drone;
                    } else {
                        CreateHQLog($"New drone detected: {drone.Id}", STULogType.INFO);
                        MiningDrones.Add(drone.Id, drone);
                    }
                } catch (Exception e) {
                    CreateHQLog($"Error processing drone telemetry messages: {e.Message}", STULogType.ERROR);
                }
            }

        }

        void WriteAllLogScreens() {
            foreach (var screen in LogSubscribers) {
                screen.Update();
            }
        }

        void PublishExternalLog(STULog log) {
            foreach (var subscriber in LogSubscribers) {
                subscriber.FlightLogs.Enqueue(log);
            }
        }

        void CreateHQLog(string message, string type) {
            STULog log = new STULog {
                Sender = AUTO_MINER_VARIABLES.AUTO_MINER_HQ_NAME,
                Message = message,
                Type = type,
            };
            foreach (var subscriber in LogSubscribers) {
                subscriber.FlightLogs.Enqueue(log);
            }
        }

        /// <summary>
        /// Finds an available drone and sends it to the job site; if no drones are available, the job is not dispatched.
        /// Note that this assumes the incoming log has both JobSite and JobPlane already in the metadata.
        /// </summary>
        /// <param name="log"></param>
        void DispatchDrone(STULog log) {
            // Find an available drone
            CreateHQLog("Dispatching drone", STULogType.INFO);
            foreach (var drone in MiningDrones) {
                if (drone.Value.State == MinerState.IDLE) {
                    // Send the drone the new job
                    CreateHQLog($"Sending drone {drone.Key} to new job site", STULogType.INFO);
                    log.Metadata.Add("DroneId", drone.Key);
                    HQToDroneBroadcaster.Log(new STULog {
                        Sender = AUTO_MINER_VARIABLES.AUTO_MINER_HQ_NAME,
                        Message = "SetJobSite",
                        Type = STULogType.INFO,
                        Metadata = log.Metadata
                    });
                    CreateHQLog($"Message sent", STULogType.OK);
                    break;
                }
            }
            CreateHQLog("No drones available, cannot dispatch job", STULogType.ERROR);
        }

    }
}
