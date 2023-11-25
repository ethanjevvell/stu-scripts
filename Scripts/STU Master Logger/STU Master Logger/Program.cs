using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private string MASTER_LOGGER_CHANNEL = "LHQ_MASTER_LOGGER";
        public string HEADER_TEXT = "LHQ Systems";

        IMyBroadcastListener listener;
        MyIGCMessage message;
        IMyBlockGroup masterBlockGroup;
        List<IMyTerminalBlock> subscribers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> auxSubscribers = new List<IMyTerminalBlock>();
        LogPublisher publisher;

        public Program() {
            listener = IGC.RegisterBroadcastListener(MASTER_LOGGER_CHANNEL);
            try {
                masterBlockGroup = GridTerminalSystem.GetBlockGroupWithName("MASTER_LOGGER_LCDS");
                masterBlockGroup.GetBlocks(subscribers);
            } catch {
                Echo("No blocks in MASTER_LOGGER_LCDS group found.");
            }

            // get every block where the custom data contains 'MASTER_LOGGER'
            try {
                GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(auxSubscribers, block => block.CustomData.Contains("MASTER_LOGGER:"));
            } catch {
                Echo("No blocks with MASTER_LOGGER in custom data found.");
            }
            publisher = new LogPublisher(subscribers, auxSubscribers, HEADER_TEXT);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main() {
            Echo($"Last runtime: {Runtime.LastRunTimeMs} ms");
            if (listener.HasPendingMessage) {
                message = listener.AcceptMessage();
                STULog newLog;
                try {
                    newLog = STULog.Deserialize(message.Data.ToString());
                    if (newLog.Metadata != null) {
                        Echo("Has metadata:\n");
                        foreach (KeyValuePair<string, string> entry in newLog.Metadata) {
                            Echo($"{entry.Key}: {entry.Value}");
                        }
                    }
                    publisher.Publish(newLog);
                } catch (System.ArgumentException) {
                    // This should never happen; log validity is enforced at the object level.
                    Echo($"Received malformed log from sender {message.Source}");
                }
            }

        }
    }
}
