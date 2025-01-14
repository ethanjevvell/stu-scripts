
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        const int RECENTLY_DESTROYED_TIMER = 30;

        private const string LIGMA_MISSION_CONTROL_MAIN_LCDS_GROUP = "LIGMA_MISSION_CONTROL_MAIN_LCDS";
        private const string LIGMA_MISSION_CONTROL_LOG_LCDS_GROUP = "LIGMA_MISSION_CONTROL_LOG_LCDS";
        private const string LIGMA_MISSION_CONTROL_AUX_MAIN_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_MAIN_LCD:";
        private const string LIGMA_MISSION_CONTROL_AUX_LOG_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_LOG_LCD:";

        private const string telemetryRecordHeader = "Timestamp, Phase, V_x, V_y, V_z, A_x, A_y, A_z, Fuel, Power\n";

        MyIni _ini = new MyIni();

        IMyBroadcastListener _telemetryListener;
        IMyBroadcastListener _logListener;
        IMyBroadcastListener _targetListener;
        MyIGCMessage _incomingMessage;

        List<LogLCD> _logSubscribers = new List<LogLCD>();
        List<MainLCD> _mainSubscribers = new List<MainLCD>();

        // Holds targets detected by the GOOCH system
        Queue<Dictionary<string, string>> _targets = new Queue<Dictionary<string, string>>();
        // Only used temporarily to hold idle missiles
        Queue<Dictionary<string, string>> _idleMissiles = new Queue<Dictionary<string, string>>();
        // Maps target ids to missile ids when a missile is dispatched
        Dictionary<string, string> _targetToLIGMA = new Dictionary<string, string>();
        // Maps missile ids to telemetry data
        Dictionary<string, Dictionary<string, string>> _LIGMATelemetryMap = new Dictionary<string, Dictionary<string, string>>();
        // Used to store incoming telemetry data temporarily
        Dictionary<string, Dictionary<string, string>> _incomingTelemetryMap = new Dictionary<string, Dictionary<string, string>>();
        // Used to store recently destroyed targets
        Dictionary<string, double> _recentlyDestroyedTargetsTimestamps = new Dictionary<string, double>();
        // Used to store recently destroyed LIGMAs
        Dictionary<string, double> _recentlyDestroyedLIGMAsTimestamps = new Dictionary<string, double>();

        StringBuilder telemetryRecords = new StringBuilder();

        // Holds log data temporarily for each run
        STULog _incomingLog;
        STULog _outgoingLog;
        Dictionary<string, double> _tempTimestampUpdates = new Dictionary<string, double>();
        List<string> _tempTargetsToRemove = new List<string>();
        List<string> _tempLigmasToRemove = new List<string>();
        Dictionary<string, string> _tempMetadata;

        int TELEMETRY_WRITE_COUNTER = 0;
        int TELEMETRY_WRITE_INTERVAL = 15;

        public Program() {
            _logListener = IGC.RegisterBroadcastListener(LIGMA_VARIABLES.LIGMA_LOG_BROADCASTER);
            _telemetryListener = IGC.RegisterBroadcastListener(LIGMA_VARIABLES.LIGMA_TELEMETRY_BROADCASTER);
            _targetListener = IGC.RegisterBroadcastListener(LIGMA_VARIABLES.LIGMA_GOOCH_TARGET_BROADCASTER);
            _logSubscribers = DiscoverLogSubscribers();
            _mainSubscribers = DiscoverMainSubscribers();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {

            try {
                foreach (var timestamp in _recentlyDestroyedTargetsTimestamps) {
                    Echo($"Target: {timestamp.Key}, {timestamp.Value}");
                }
                foreach (var timestamp in _recentlyDestroyedLIGMAsTimestamps) {
                    Echo($"LIGMA: {timestamp.Key}, {timestamp.Value}");
                }
                UpdateRecentlyDestroyedLists();
                HandleIncomingBroadcasts();
                DispatchMissiles();

                // Write telemetry to CustomData every TELEMETRY_WRITE_INTERVAL runs
                if (TELEMETRY_WRITE_COUNTER >= TELEMETRY_WRITE_INTERVAL) {
                    Me.CustomData = telemetryRecords.ToString();
                    TELEMETRY_WRITE_COUNTER = 0;
                } else {
                    TELEMETRY_WRITE_COUNTER++;
                }
            } catch (Exception e) {
                CreateHQLog($"Error in main loop: {e}. Terminating script.", STULogType.ERROR);
                Runtime.UpdateFrequency = UpdateFrequency.None;
            } finally {
                WriteAllLogScreens();
                WriteAllMainScreens();
            }
        }

        public void HandleIncomingBroadcasts() {
            HandleIncomingTargetBroadcasts();
            HandleIncomingTelemetry();
            HandleIncomingLogs();
        }

        public void HandleIncomingTargetBroadcasts() {
            while (_targetListener.HasPendingMessage) {
                _incomingMessage = _targetListener.AcceptMessage();
                try {
                    _incomingLog = STULog.Deserialize(_incomingMessage.Data.ToString());
                    if (_targetToLIGMA.ContainsKey(_incomingLog.Metadata["EntityId"])) {
                        try {
                            SendTargetDataToLIGMA(_targetToLIGMA[_incomingLog.Metadata["EntityId"]], _incomingLog.Metadata);
                        } catch {
                            CreateHQLog("Error parsing incoming target data", STULogType.ERROR);
                        }
                    } else if (!_recentlyDestroyedTargetsTimestamps.ContainsKey(_incomingLog.Metadata["EntityId"])) {
                        _targets.Enqueue(_incomingLog.Metadata);
                    }
                } catch {
                    _incomingLog = new STULog {
                        Sender = LIGMA_VARIABLES.LIGMA_RECONNOITERER_NAME,
                        Message = $"Received invalid message: {_incomingLog.Serialize()}",
                        Type = STULogType.ERROR
                    };
                }
            }
        }

        public void HandleIncomingLogs() {
            while (_logListener.HasPendingMessage) {
                MyIGCMessage message = _logListener.AcceptMessage();
                try {
                    _incomingLog = STULog.Deserialize(message.Data.ToString());
                    if (!string.IsNullOrEmpty(_incomingLog.Message)) {
                        PublishExternalLog(_incomingLog);
                        if (_incomingLog.Message == LIGMA_VARIABLES.COMMANDS.SendGoodbye) {
                            CreateHQLog($"Removing LIGMA {message.Source} from active duty", STULogType.WARNING);
                            HandleGoodbye(message.Source);
                        }
                    } else {
                        CreateHQLog("Message on the drone log channel did not contain a message field", STULogType.ERROR);
                    }
                } catch (Exception e) {
                    CreateHQLog($"Error processing incoming drone log: {e.Message}", STULogType.ERROR);
                }
            }
        }

        public void SendTargetDataToLIGMA(string missileId, Dictionary<string, string> target) {
            STULog commandLog = new STULog {
                Sender = LIGMA_VARIABLES.LIGMA_HQ_NAME,
                Message = LIGMA_VARIABLES.COMMANDS.UpdateTargetData,
                Type = STULogType.INFO,
                Metadata = target
            };
            long id;
            if (long.TryParse(missileId, out id)) {
                IGC.SendUnicastMessage(id, LIGMA_VARIABLES.LIGMA_HQ_TELEMETRY_BROADCASTER, commandLog.Serialize());
            } else {
                CreateHQLog($"Failed to parse missile id: {missileId}", STULogType.ERROR);
            }
        }

        public void WriteTelemetry() {
            _tempMetadata = _incomingLog.Metadata;
            Vector3D parsedVelocity;
            Vector3D parsedAcceleration;
            parsedVelocity = Vector3D.TryParse(_tempMetadata["VelocityComponents"], out parsedVelocity) ? parsedVelocity : Vector3D.Zero;
            parsedAcceleration = Vector3D.TryParse(_tempMetadata["AccelerationComponents"], out parsedAcceleration) ? parsedAcceleration : Vector3D.Zero;
            telemetryRecords.Append($"{_tempMetadata["Timestamp"]}, {_tempMetadata["Phase"]}, {parsedVelocity.X}, {parsedVelocity.Y}, {parsedVelocity.Z}, {parsedAcceleration.X}, {parsedAcceleration.Y}, {parsedAcceleration.Z}, {_tempMetadata["CurrentFuel"]}, {_tempMetadata["CurrentPower"]}\n");
        }


        public void HandleIncomingTelemetry() {

            _incomingTelemetryMap.Clear();

            while (_telemetryListener.HasPendingMessage) {
                MyIGCMessage message = _telemetryListener.AcceptMessage();
                try {
                    _incomingLog = STULog.Deserialize(message.Data.ToString());

                    // Proceed to deserialize the LIGMA data
                    string id;
                    _incomingLog.Metadata.TryGetValue("Id", out id);

                    if (id == null) {
                        throw new Exception("Received null id!");
                    }

                    if (_incomingTelemetryMap.ContainsKey(id)) {
                        return;
                    }

                    _incomingTelemetryMap.Add(id, _incomingLog.Metadata);

                    // Update or add the drone to the LIGMA dictionary
                    if (_LIGMATelemetryMap.ContainsKey(id)) {
                        if (_LIGMATelemetryMap[id]["Phase"] == "Missing") {
                            CreateHQLog($"LIGMA {id} has returned", STULogType.OK);
                        }
                        _LIGMATelemetryMap[id] = _incomingLog.Metadata;
                    } else if (!_recentlyDestroyedLIGMAsTimestamps.ContainsKey(id)) {
                        CreateHQLog($"New LIGMA detected: {id}", STULogType.OK);
                        _LIGMATelemetryMap.Add(id, _incomingLog.Metadata);
                    } else {
                    }

                } catch (Exception e) {
                    CreateHQLog($"Error processing LIGMA telemetry messages: {e.Message}", STULogType.ERROR);
                }

            }

            // Check for missing missiles
            foreach (var missile in _LIGMATelemetryMap) {
                if (!_incomingTelemetryMap.ContainsKey(missile.Key) && _LIGMATelemetryMap[missile.Key]["Phase"] != "Missing") {
                    CreateHQLog($"LIGMA {missile.Key} is missing", STULogType.WARNING);
                    missile.Value["Phase"] = "Missing";
                }
            }

        }

        void HandleGoodbye(long rawId) {
            string ligmaId = rawId.ToString();
            _LIGMATelemetryMap.Remove(ligmaId);
            // Get the id of the target LIGMA is assigned to
            string targetId = null;
            foreach (var target in _targetToLIGMA) {
                if (target.Value == ligmaId) {
                    targetId = target.Key;
                    break;
                }
            }
            if (targetId != null && _targetToLIGMA.ContainsKey(targetId)) {
                _targetToLIGMA.Remove(targetId);
                _recentlyDestroyedTargetsTimestamps.Add(targetId, 0);
                _recentlyDestroyedLIGMAsTimestamps.Add(ligmaId, 0);
                CreateHQLog($"Acknowledged LIGMA {ligmaId} has been removed from target {targetId}", STULogType.OK);
            } else {
                CreateHQLog($"Failed to remove LIGMA {ligmaId} from target {targetId}", STULogType.ERROR);
            }
        }

        void DispatchMissiles() {
            _idleMissiles.Clear();
            foreach (var missile in _LIGMATelemetryMap) {
                if (missile.Value["Phase"] == "Idle") {
                    _idleMissiles.Enqueue(missile.Value);
                }
            }
            while (_targets.Count > 0 && _idleMissiles.Count > 0) {
                var target = _targets.Dequeue();
                var missile = _idleMissiles.Dequeue();
                Dispatch(missile, target);
            }
        }

        void Dispatch(Dictionary<string, string> missile, Dictionary<string, string> target) {
            SendTargetDataToLIGMA(missile["Id"], target);
            SendLaunchCommand(missile["Id"]);
            CreateHQLog($"Dispatched LIGMA {missile["Id"]} to target {target["EntityId"]}", STULogType.OK);
            _targetToLIGMA[target["EntityId"]] = missile["Id"];
            missile["Phase"] = "Launch";
        }

        void SendLaunchCommand(string missileId) {
            STULog commandLog = new STULog {
                Sender = LIGMA_VARIABLES.LIGMA_HQ_TELEMETRY_BROADCASTER,
                Message = LIGMA_VARIABLES.COMMANDS.Launch,
                Type = STULogType.INFO
            };
            long id;
            if (long.TryParse(missileId, out id)) {
                IGC.SendUnicastMessage(id, LIGMA_VARIABLES.LIGMA_HQ_TELEMETRY_BROADCASTER, commandLog.Serialize());
            } else {
                CreateHQLog($"Failed to parse missile id: {missileId}", STULogType.ERROR);
            }
        }


        List<LogLCD> DiscoverLogSubscribers() {
            List<IMyTextPanel> logPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(
                logPanels,
                block => MyIni.HasSection(block.CustomData, LIGMA_VARIABLES.LIGMA_HQ_LOG_SUBSCRIBER_TAG)
                && block.CubeGrid == Me.CubeGrid);
            return new List<LogLCD>(logPanels.ConvertAll(panel => {
                MyIniParseResult result;
                if (!_ini.TryParse(panel.CustomData, out result)) {
                    throw new Exception($"Error parsing log configuration: {result}");
                }
                int displayIndex = _ini.Get(LIGMA_VARIABLES.LIGMA_HQ_LOG_SUBSCRIBER_TAG, "DisplayIndex").ToInt32(0);
                double fontSize = _ini.Get(LIGMA_VARIABLES.LIGMA_HQ_LOG_SUBSCRIBER_TAG, "FontSize").ToDouble(1.0);
                string font = _ini.Get(LIGMA_VARIABLES.LIGMA_HQ_LOG_SUBSCRIBER_TAG, "Font").ToString("Monospace");
                return new LogLCD(panel, displayIndex, font, (float)fontSize);
            }));
        }

        List<MainLCD> DiscoverMainSubscribers() {
            List<IMyTextPanel> mainPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(
                mainPanels,
                block => MyIni.HasSection(block.CustomData, LIGMA_VARIABLES.LIGMA_HQ_MAIN_SUBSCRIBER_TAG)
                && block.CubeGrid == Me.CubeGrid
            );
            return new List<MainLCD>(mainPanels.ConvertAll(panel => {
                return new MainLCD(panel, 0);
            }));
        }

        void WriteAllLogScreens() {
            _logSubscribers.ForEach(subscriber => {
                Echo(subscriber.Logs.Count.ToString());
                subscriber.StartFrame();
                subscriber.WriteWrappableLogs(subscriber.Logs);
                subscriber.EndAndPaintFrame();
            });
        }

        private void UpdateDestroyedEntities(Dictionary<string, double> timestamps, List<string> removeList) {
            _tempTimestampUpdates.Clear();
            removeList.Clear();

            // Collect updates
            foreach (var entity in timestamps) {
                double newValue = entity.Value + Runtime.TimeSinceLastRun.TotalSeconds;
                _tempTimestampUpdates[entity.Key] = newValue;

                if (newValue >= RECENTLY_DESTROYED_TIMER) {
                    removeList.Add(entity.Key);
                }
            }

            // Apply updates
            foreach (var update in _tempTimestampUpdates) {
                timestamps[update.Key] = update.Value;
            }

            // Remove old entities
            foreach (var key in removeList) {
                timestamps.Remove(key);
            }
        }

        void UpdateRecentlyDestroyedLists() {
            UpdateDestroyedEntities(_recentlyDestroyedTargetsTimestamps, _tempTargetsToRemove);
            UpdateDestroyedEntities(_recentlyDestroyedLIGMAsTimestamps, _tempLigmasToRemove);
        }

        void WriteAllMainScreens() {
            foreach (var missile in _LIGMATelemetryMap) {
                Echo(missile.Value["Id"]);
            }
        }

        void CreateHQLog(string message, string type) {
            STULog log = new STULog {
                Sender = LIGMA_VARIABLES.LIGMA_HQ_NAME,
                Message = message,
                Type = type,
            };
            foreach (var subscriber in _logSubscribers) {
                subscriber.Logs.Enqueue(log);
            }
        }

        void PublishExternalLog(STULog log) {
            foreach (var subscriber in _logSubscribers) {
                subscriber.Logs.Enqueue(log);
            }
        }

    }
}
