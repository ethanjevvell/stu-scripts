using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyBroadcastListener DroneTelemetryListener;
        IMyBroadcastListener DroneLogListener;
        IMyBroadcastListener ReconNewJobListener;

        MyIGCMessage IGCMessage;

        // MAIN LCDS
        List<IMyTerminalBlock> MainSubscribers = new List<IMyTerminalBlock>();

        // LOG LCDS
        List<LogLCD> LogSubscribers = new List<LogLCD>();

        Queue<MyTuple<Vector3D, PlaneD>> JobQueue = new Queue<MyTuple<Vector3D, PlaneD>>();

        Dictionary<string, MiningDroneData> MiningDrones = new Dictionary<string, MiningDroneData>();

        // Holds log data temporarily for each run
        STULog IncomingLog;
        STULog OutgoingLog;

        Dictionary<string, MiningDroneData> IncomingDroneTelemetryData = new Dictionary<string, MiningDroneData>();

        public Program() {

            // Drone listeners
            DroneTelemetryListener = IGC.RegisterBroadcastListener(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_TELEMETRY_CHANNEL);
            DroneLogListener = IGC.RegisterBroadcastListener(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_LOG_CHANNEL);

            // Recon listeners
            ReconNewJobListener = IGC.RegisterBroadcastListener(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_RECON_JOB_LISTENER);

            // Initialize HQ to drone broadcaster
            LogSubscribers = DiscoverSubscribers(GridTerminalSystem);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {

            try {
                UpdateDroneTelemetry();
                HandleIncomingDroneLogs();
                HandleNewIncomingJobs();
                DispatchDroneIfAvailable();
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

            logLCD = null;

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

                    float fontSize;
                    int panelIndex;

                    if (float.TryParse(kvp[2], out fontSize) && int.TryParse(kvp[1], out panelIndex)) {
                        if (fontSize < minFontSize || fontSize > maxFontSize) {
                            throw new Exception($"Invalid font size for {block.Name}; fontSize must be between {minFontSize} and {maxFontSize}");
                        }
                        logLCD = new LogLCD(block, int.Parse(kvp[1]), "Monospace", fontSize);
                        break;
                    } else {
                        throw new Exception($"Error parsing log configuration");
                    }
                }
            }
        }

        void HandleIncomingDroneLogs() {
            while (DroneLogListener.HasPendingMessage) {
                MyIGCMessage message = DroneLogListener.AcceptMessage();
                try {
                    IncomingLog = STULog.Deserialize(message.Data.ToString());
                    if (!string.IsNullOrEmpty(IncomingLog.Message)) {
                        // If it's just a message, publish it to the log screens and move on to the next incoming message
                        PublishExternalLog(IncomingLog);
                    } else {
                        CreateHQLog("Message on the drone log channel did not contain a message field", STULogType.ERROR);
                    }
                } catch (Exception e) {
                    CreateHQLog($"Error processing incoming drone log: {e.Message}", STULogType.ERROR);
                }
            }
        }

        /// <summary>
        /// Processes incoming drone telemetry messages to determine the state of each drone, discover new drones, and update existing drones.
        /// </summary>
        void UpdateDroneTelemetry() {

            IncomingDroneTelemetryData.Clear();

            while (DroneTelemetryListener.HasPendingMessage) {
                MyIGCMessage message = DroneTelemetryListener.AcceptMessage();
                try {
                    IncomingLog = STULog.Deserialize(message.Data.ToString());

                    // Check if Metadata is not null and contains the key "MinerDroneData"
                    if (!IncomingLog.Metadata.ContainsKey("MinerDroneData")) {
                        CreateHQLog("Incoming telemetry message does not contain MinerDroneData", STULogType.ERROR);
                        continue; // Skip processing this message
                    }

                    // Proceed to deserialize the drone data
                    MiningDroneData drone = MiningDroneData.Deserialize(IncomingLog.Metadata["MinerDroneData"]);
                    IncomingDroneTelemetryData.Add(drone.Id, drone);

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

            // Check for missing drones
            foreach (var drone in MiningDrones) {
                if (!IncomingDroneTelemetryData.ContainsKey(drone.Key)) {
                    CreateHQLog($"Drone {drone.Key} is missing", STULogType.WARNING);
                    drone.Value.State = MinerState.MISSING;
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

        void HandleNewIncomingJobs() {
            while (ReconNewJobListener.HasPendingMessage) {
                IGCMessage = ReconNewJobListener.AcceptMessage();
                try {
                    IncomingLog = STULog.Deserialize(IGCMessage.Data.ToString());
                    AddJobToQueue(IncomingLog);
                    PublishExternalLog(IncomingLog);
                } catch (Exception e) {
                    CreateHQLog($"Error processing incoming job: {e.Message}", STULogType.ERROR);
                }
            }
        }

        void AddJobToQueue(STULog log) {
            Vector3D jobSite = MiningDroneData.DeserializeVector3D(log.Metadata["JobSite"]);
            PlaneD jobPlane = MiningDroneData.DeserializePlaneD(log.Metadata["JobPlane"]);
            JobQueue.Enqueue(new MyTuple<Vector3D, PlaneD>(jobSite, jobPlane));
            CreateHQLog($"Job added to queue: {jobSite}", STULogType.INFO);
        }

        void DispatchDroneIfAvailable() {
            var idleDrones = MiningDrones.Values.Where(d => d.State == MinerState.IDLE).ToList();
            while (JobQueue.Count > 0 && idleDrones.Count > 0) {
                var job = JobQueue.Dequeue();
                var drone = idleDrones.Pop();
                DispatchDrone(drone.Id, job.Item1, job.Item2);
                CreateHQLog($"Drone {drone.Id} dispatched to {job.Item1}", STULogType.INFO);
                drone.State = MinerState.FLY_TO_JOB_SITE;
            }
        }

        void DispatchDrone(string droneId, Vector3D jobSite, PlaneD jobPlane) {
            OutgoingLog = new STULog {
                Sender = AUTO_MINER_VARIABLES.AUTO_MINER_HQ_NAME,
                Message = "SetJobSite",
                Type = STULogType.INFO,
                Metadata = new Dictionary<string, string> {
                    { "JobSite", MiningDroneData.FormatVector3D(jobSite) },
                    { "JobPlane", MiningDroneData.FormatPlaneD(jobPlane) }
                }
            };
            long parsedDroneId;
            if (long.TryParse(droneId, out parsedDroneId)) {
                IGC.SendUnicastMessage(parsedDroneId, AUTO_MINER_VARIABLES.AUTO_MINER_DRONE_COMMAND_CHANNEL, OutgoingLog.Serialize());
            } else {
                CreateHQLog($"Invalid drone ID: {droneId}", STULogType.ERROR);
            }
        }

    }
}
