using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyBroadcastListener _nodeLogListener;
        IMyBroadcastListener _nodeTargetListener;

        public Program() {
            _nodeLogListener = IGC.RegisterBroadcastListener("gooch-node-log");
            _nodeTargetListener = IGC.RegisterBroadcastListener("gooch-node-target");
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        public void Main() {
            while (_nodeLogListener.HasPendingMessage) {
                MyIGCMessage message = _nodeLogListener.AcceptMessage();
                STULog log = STULog.Deserialize(message.Data.ToString());
                Echo($"{log.Sender}: {log.Message}");
            }
            while (_nodeTargetListener.HasPendingMessage) {
                MyIGCMessage message = _nodeTargetListener.AcceptMessage();
                STULog sTULog = STULog.Deserialize(message.Data.ToString());
                MyDetectedEntityInfo detectedEntity = STURaycaster.DeserializeHitInfo(sTULog.Metadata);
                Echo($"{detectedEntity.Name}");
            }
        }
    }
}
