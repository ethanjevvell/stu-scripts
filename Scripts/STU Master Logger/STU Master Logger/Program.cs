using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private string MASTER_LOGGER_CHANNEL = "LHQ_MASTER_LOGGER";

        IMyBroadcastListener listener;
        MyIGCMessage message;
        IMyBlockGroup masterBlockGroup;
        List<IMyTerminalBlock> subscribers = new List<IMyTerminalBlock>();
        LogPublisher publisher;

        public Program() {
            listener = IGC.RegisterBroadcastListener(MASTER_LOGGER_CHANNEL);
            masterBlockGroup = GridTerminalSystem.GetBlockGroupWithName("MASTER_LOGGER_LCDS");
            masterBlockGroup.GetBlocks(subscribers);

            publisher = new LogPublisher(subscribers);
            publisher.ClearPanels();

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main() {
            Echo($"Last runtime: {Runtime.LastRunTimeMs} ms");
            if (listener.HasPendingMessage) {
                message = listener.AcceptMessage();
                STULog newLog = STULog.Deserialize(message.Data.ToString());
                if (newLog != null) {
                    publisher.Publish(newLog);
                } else {
                    Echo($"Received malformed log from sender {message.Source}");
                }
            }
        }

    }
}
