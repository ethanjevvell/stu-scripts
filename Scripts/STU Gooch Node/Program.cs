using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {

    partial class Program : MyGridProgram {

        const string GOOCH_NODE_LOG_CHANNEL = "gooch-node-log";
        const string GOOCH_NODE_TARGET_CHANNEL = "gooch-node-target";

        MyIni _ini;
        IMyTurretControlBlock _customTurrentController;

        IEnumerator<bool> _targetScanner;
        // For sending log messages to HQ
        STUMasterLogBroadcaster _logBroadcaster;
        // For streaming target telemetry
        STUMasterLogBroadcaster _targetBroadcaster;

        STULog _tempOutgoingLog = new STULog();
        MyDetectedEntityInfo _tempDetectedEntity;

        public Program() {

            _ini = new MyIni();
            _logBroadcaster = new STUMasterLogBroadcaster(LIGMA_VARIABLES.LIGMA_GOOCH_LOG_BROADCASTER, IGC, TransmissionDistance.AntennaRelay);
            _targetBroadcaster = new STUMasterLogBroadcaster(LIGMA_VARIABLES.LIGMA_GOOCH_TARGET_BROADCASTER, IGC, TransmissionDistance.AntennaRelay);
            _customTurrentController = GridTerminalSystem.GetBlockWithName("NODE CONTROLLER") as IMyTurretControlBlock;
            if (_customTurrentController == null) {
                Echo("Custom Turret Controller not found");
                return;
            }

            ConfigureTurretController(_customTurrentController, true);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

        }

        public void Main() {

            ScanForTargets();

        }

        IEnumerable<bool> ScanCoroutine() {

            while (true) {
                _tempDetectedEntity = _customTurrentController.GetTargetedEntity();
                if (!_tempDetectedEntity.IsEmpty()) {
                    Echo("TARGET LOCK -- TRANSMITTING");
                    StreamTargetData(_tempDetectedEntity);
                    Echo(_tempOutgoingLog.Metadata["Name"]);
                }
                yield return true;
            }

        }

        void ScanForTargets() {
            if (_targetScanner == null) {
                _targetScanner = ScanCoroutine().GetEnumerator();
            }
            bool hasMore = _targetScanner.MoveNext();
            if (hasMore) {
                return;
            } else {
                _targetScanner.Dispose();
                _targetScanner = null;
            }
        }

        void ConfigureTurretController(IMyTurretControlBlock controller, bool debug = false) {
            if (debug) {
                controller.TargetFriends = true;
            } else {
                controller.TargetFriends = false;
            }
            controller.TargetLargeGrids = true;
            controller.TargetSmallGrids = true;
            controller.TargetStations = true;
            controller.TargetNeutrals = true;
        }

        void StreamTargetData(MyDetectedEntityInfo targetData) {
            _tempOutgoingLog = new STULog() {
                Message = "",
                Sender = Me.CustomName,
                Type = STULogType.INFO,
                Metadata = STURaycaster.GetHitInfoDictionary(targetData)
            };
            _targetBroadcaster.Log(_tempOutgoingLog);
        }
    }

}
