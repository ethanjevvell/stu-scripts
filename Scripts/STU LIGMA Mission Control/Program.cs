
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_LCDS_GROUP = "LIGMA_MISSION_CONTROL_LCDS";
        private const string LIGMA_MISSION_CONTROL_AUX_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_LCD:";

        private const string LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL = "LIGMA_MISSION_CONTROL";
        private const string LIGMA_VEHICLE_BROADCASTER_CHANNEL = "LIGMA_VEHICLE_CONTROL";

        IMyBroadcastListener listener;
        MyIGCMessage message;
        IMyBlockGroup masterBlockGroup;
        List<IMyTerminalBlock> subscribers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> auxSubscribers = new List<IMyTerminalBlock>();
        LogPublisher publisher;

        public Program() {
            listener = IGC.RegisterBroadcastListener(LIGMA_VEHICLE_BROADCASTER_CHANNEL);
            try {
                masterBlockGroup = GridTerminalSystem.GetBlockGroupWithName(LIGMA_MISSION_CONTROL_LCDS_GROUP);
                masterBlockGroup.GetBlocks(subscribers);
            } catch {
                Echo($"No blocks in {LIGMA_MISSION_CONTROL_LCDS_GROUP} group found.");
            }

            try {
                GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(auxSubscribers, block => block.CustomData.Contains(LIGMA_MISSION_CONTROL_AUX_LCD_TAG));
            } catch {
                Echo($"No blocks with {LIGMA_MISSION_CONTROL_AUX_LCD_TAG} in custom data found.");
            }

            publisher = new LogPublisher(subscribers, auxSubscribers);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {
            Echo($"Last runtime: {Runtime.LastRunTimeMs} ms");

            if (argument == "DETONATE") {
                IGC.SendBroadcastMessage(LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL, "DETONATE", TransmissionDistance.AntennaRelay);
            }

            if (argument == "LAUNCH") {
                Echo("Launching...");
                IGC.SendBroadcastMessage(LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL, "LAUNCH", TransmissionDistance.AntennaRelay);
            }

            if (listener.HasPendingMessage) {
                message = listener.AcceptMessage();
                STULog newLog;
                try {
                    newLog = STULog.Deserialize(message.Data.ToString());
                    publisher.UpdateDisplays(newLog);
                } catch (System.ArgumentException) {
                    Echo($"Received malformed log from sender {message.Source}");
                }
            } else {
                // TODO: Implement display logic for missile having "NO SIGNAL" etc.
            }

        }
    }
}
