using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        MyIni _ini;

        IMyBroadcastListener _droneTelemetryListener;
        IMyBroadcastListener _droneLogListener;
        IMyBroadcastListener _reconNewJobListener;

        MyIGCMessage _IGCMessage;

        List<LogLCD> _logSubscribers = new List<LogLCD>();
        List<MainLCD> _mainSubscribers = new List<MainLCD>();

        Queue<MyTuple<Vector3D, PlaneD, int>> _jobQueue = new Queue<MyTuple<Vector3D, PlaneD, int>>();

        Dictionary<string, MiningDroneData> _miningDrones = new Dictionary<string, MiningDroneData>();

        // Holds log data temporarily for each run
        STULog _tempIncomingLog;
        STULog _tempOutgoingLog;

        Dictionary<string, MiningDroneData> _tempIncomingDroneTelemetryData = new Dictionary<string, MiningDroneData>();

        public Program() {

            _ini = new MyIni();

            // Drone listeners
            _droneTelemetryListener = IGC.RegisterBroadcastListener(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_TELEMETRY_CHANNEL);
            _droneLogListener = IGC.RegisterBroadcastListener(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_DRONE_LOG_CHANNEL);

            // Recon listeners
            _reconNewJobListener = IGC.RegisterBroadcastListener(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_RECON_JOB_LISTENER);

            // Initialize HQ to drone broadcaster
            _logSubscribers = DiscoverLogSubscribers();
            _mainSubscribers = DiscoverMainSubscribers();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            ParseStorage(Storage);
        }

        public void Main(string argument) {

            try {
                UpdateDroneTelemetry();
                HandleIncomingDroneLogs();
                HandleNewIncomingJobs();
                DispatchDroneIfAvailable();
                WriteAllMainScreens();
            } catch (Exception e) {
                CreateHQLog($"Error in Main(): {e.Message}. Terminating script.", STULogType.ERROR);
                Runtime.UpdateFrequency = UpdateFrequency.None;
            } finally {
                WriteAllLogScreens();
            }

        }

        public void Save() {
            // Assume this has already been parsed or used
            _ini.Clear();
            _ini.Set("HQ_STORAGE", "JobQueue", SerializeQueue(_jobQueue));
            // Persist to disk
            Storage = _ini.ToString();
        }

        List<LogLCD> DiscoverLogSubscribers() {
            List<IMyTextPanel> logPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(
                logPanels,
                block => MyIni.HasSection(block.CustomData, AUTO_MINER_VARIABLES.AUTO_MINER_LOG_SUBSCRIBER_TAG)
                && block.CubeGrid == Me.CubeGrid);
            return new List<LogLCD>(logPanels.ConvertAll(panel => {
                MyIniParseResult result;
                if (!_ini.TryParse(panel.CustomData, out result)) {
                    throw new Exception($"Error parsing log configuration: {result}");
                }
                int displayIndex = _ini.Get(AUTO_MINER_VARIABLES.AUTO_MINER_LOG_SUBSCRIBER_TAG, "DisplayIndex").ToInt32(0);
                double fontSize = _ini.Get(AUTO_MINER_VARIABLES.AUTO_MINER_LOG_SUBSCRIBER_TAG, "FontSize").ToDouble(1.0);
                string font = _ini.Get(AUTO_MINER_VARIABLES.AUTO_MINER_LOG_SUBSCRIBER_TAG, "Font").ToString("Monospace");
                return new LogLCD(panel, displayIndex, font, (float)fontSize);
            }));
        }

        List<MainLCD> DiscoverMainSubscribers() {
            List<IMyTextPanel> mainPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(
                mainPanels,
                block => MyIni.HasSection(block.CustomData, AUTO_MINER_VARIABLES.AUTO_MINER_HQ_MAIN_SUBSCRIBER_TAG)
                && block.CubeGrid == Me.CubeGrid
            );
            return new List<MainLCD>(mainPanels.ConvertAll(panel => {
                return new MainLCD(panel, 0);
            }));
        }

        void HandleIncomingDroneLogs() {
            while (_droneLogListener.HasPendingMessage) {
                MyIGCMessage message = _droneLogListener.AcceptMessage();
                try {
                    _tempIncomingLog = STULog.Deserialize(message.Data.ToString());
                    if (!string.IsNullOrEmpty(_tempIncomingLog.Message)) {
                        // If it's just a message, publish it to the log screens and move on to the next incoming message
                        PublishExternalLog(_tempIncomingLog);
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

            _tempIncomingDroneTelemetryData.Clear();

            while (_droneTelemetryListener.HasPendingMessage) {
                MyIGCMessage message = _droneTelemetryListener.AcceptMessage();
                try {
                    _tempIncomingLog = STULog.Deserialize(message.Data.ToString());

                    // Check if Metadata is not null and contains the key "MinerDroneData"
                    if (!_tempIncomingLog.Metadata.ContainsKey("MinerDroneData")) {
                        CreateHQLog("Incoming telemetry message does not contain MinerDroneData", STULogType.ERROR);
                        continue; // Skip processing this message
                    }

                    // Proceed to deserialize the drone data
                    MiningDroneData drone = MiningDroneData.Deserialize(_tempIncomingLog.Metadata["MinerDroneData"]);

                    if (_tempIncomingDroneTelemetryData.ContainsKey(drone.Id)) {
                        return;
                    }

                    _tempIncomingDroneTelemetryData.Add(drone.Id, drone);

                    // Update or add the drone to the MiningDrones dictionary
                    if (_miningDrones.ContainsKey(drone.Id)) {
                        if (_miningDrones[drone.Id].State == MinerState.MISSING) {
                            CreateHQLog($"Drone {drone.Id} has returned", STULogType.OK);
                        }
                        _miningDrones[drone.Id] = drone;
                    } else {
                        CreateHQLog($"New drone detected: {drone.Id}", STULogType.INFO);
                        _miningDrones.Add(drone.Id, drone);
                    }

                } catch (Exception e) {
                    CreateHQLog($"Error processing drone telemetry messages: {e.Message}", STULogType.ERROR);
                }
            }

            // Check for missing drones
            foreach (var drone in _miningDrones) {
                if (!_tempIncomingDroneTelemetryData.ContainsKey(drone.Key) && _miningDrones[drone.Key].State != MinerState.MISSING) {
                    CreateHQLog($"Drone {drone.Key} is missing", STULogType.WARNING);
                    drone.Value.State = MinerState.MISSING;
                }
            }

        }

        void WriteAllLogScreens() {
            foreach (var screen in _logSubscribers) {
                screen.Update();
            }
        }

        void WriteAllMainScreens() {
            foreach (var screen in _mainSubscribers) {
                screen.Update(_miningDrones);
            }
        }

        void PublishExternalLog(STULog log) {
            foreach (var subscriber in _logSubscribers) {
                subscriber.FlightLogs.Enqueue(log);
            }
        }

        void CreateHQLog(string message, string type) {
            STULog log = new STULog {
                Sender = AUTO_MINER_VARIABLES.AUTO_MINER_HQ_NAME,
                Message = message,
                Type = type,
            };
            foreach (var subscriber in _logSubscribers) {
                subscriber.FlightLogs.Enqueue(log);
            }
        }

        void HandleNewIncomingJobs() {
            while (_reconNewJobListener.HasPendingMessage) {
                _IGCMessage = _reconNewJobListener.AcceptMessage();
                try {
                    _tempIncomingLog = STULog.Deserialize(_IGCMessage.Data.ToString());
                    AddJobToQueue(_tempIncomingLog);
                    PublishExternalLog(_tempIncomingLog);
                } catch (Exception e) {
                    CreateHQLog($"Error processing incoming job: {e.Message}", STULogType.ERROR);
                }
            }
        }

        void AddJobToQueue(STULog log) {
            Vector3D jobSite = MiningDroneData.DeserializeVector3D(log.Metadata["JobSite"]);
            PlaneD jobPlane = MiningDroneData.DeserializePlaneD(log.Metadata["JobPlane"]);
            int jobDepth = int.Parse(log.Metadata["JobDepth"]);
            _jobQueue.Enqueue(new MyTuple<Vector3D, PlaneD, int>(jobSite, jobPlane, jobDepth));
            CreateHQLog($"Job added to queue", STULogType.INFO);
        }

        void DispatchDroneIfAvailable() {
            var idleDrones = new Stack<MiningDroneData>(_miningDrones.Values.Where(d => d.State == MinerState.IDLE));
            while (_jobQueue.Count > 0 && idleDrones.Count > 0) {
                var job = _jobQueue.Dequeue();
                var drone = idleDrones.Pop();
                DispatchDrone(drone.Id, job.Item1, job.Item2, job.Item3);
                CreateHQLog($"Drone {drone.Id} dispatched to {Vector3D.Round(job.Item1)} with depth {job.Item3}", STULogType.INFO);
                CreateHQLog($"{_jobQueue.Count} remaining jobs", STULogType.INFO);
                drone.State = MinerState.FLY_TO_JOB_SITE;
            }
        }

        void DispatchDrone(string droneId, Vector3D jobSite, PlaneD jobPlane, int jobDepth) {
            _tempOutgoingLog = new STULog {
                Sender = AUTO_MINER_VARIABLES.AUTO_MINER_HQ_NAME,
                Message = "SetJobSite",
                Type = STULogType.INFO,
                Metadata = new Dictionary<string, string> {
                    { "JobSite", MiningDroneData.FormatVector3D(jobSite) },
                    { "JobPlane", MiningDroneData.FormatPlaneD(jobPlane) },
                    { "JobDepth", jobDepth.ToString() }
                }
            };
            long parsedDroneId;
            if (long.TryParse(droneId, out parsedDroneId)) {
                IGC.SendUnicastMessage(parsedDroneId, AUTO_MINER_VARIABLES.AUTO_MINER_DRONE_COMMAND_CHANNEL, _tempOutgoingLog.Serialize());
            } else {
                CreateHQLog($"Invalid drone ID: {droneId}", STULogType.ERROR);
            }
        }

        void ParseStorage(string storage) {
            _ini.TryParse(storage);
            _jobQueue = DeserializeJobQueue(_ini.Get("HQ_STORAGE", "JobQueue").ToString(""));
            Echo(_jobQueue.Count.ToString());
        }

        string SerializeQueue(Queue<MyTuple<Vector3D, PlaneD, int>> queue) {
            return string.Join(";", queue.Select(t => $"{MiningDroneData.FormatVector3D(t.Item1)}|{MiningDroneData.FormatPlaneD(t.Item2)}|{t.Item3}"));
        }

        Queue<MyTuple<Vector3D, PlaneD, int>> DeserializeJobQueue(string queueString) {
            Queue<MyTuple<Vector3D, PlaneD, int>> queue = new Queue<MyTuple<Vector3D, PlaneD, int>>();
            if (queueString == "") {
                CreateHQLog("No queue found in storage", STULogType.WARNING);
                return queue;
            }
            try {
                queue = new Queue<MyTuple<Vector3D, PlaneD, int>>(queueString.Split(';').Select(s => {
                    var parts = s.Split('|');
                    return new MyTuple<Vector3D, PlaneD, int>(
                       MiningDroneData.DeserializeVector3D(parts[0]),
                      MiningDroneData.DeserializePlaneD(parts[1]),
                     int.Parse(parts[2]));
                }));
                CreateHQLog($"Queue deserialized successfully; {queue.Count} jobs loaded back into memory", STULogType.INFO);
                return queue;
            } catch (Exception e) {
                CreateHQLog($"Error deserializing job queue: {e.Message}", STULogType.ERROR);
                return queue;
            }
        }


    }
}
