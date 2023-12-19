
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_MAIN_LCDS_GROUP = "LIGMA_MISSION_CONTROL_MAIN_LCDS";
        private const string LIGMA_MISSION_CONTROL_LOG_LCDS_GROUP = "LIGMA_MISSION_CONTROL_LOG_LCDS";
        private const string LIGMA_MISSION_CONTROL_AUX_MAIN_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_MAIN_LCD:";
        private const string LIGMA_MISSION_CONTROL_AUX_LOG_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_LOG_LCD:";

        private const string LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL = "LIGMA_MISSION_CONTROL";
        private const string LIGMA_VEHICLE_BROADCASTER_CHANNEL = "LIGMA_VEHICLE_CONTROL";

        IMyBroadcastListener listener;
        MyIGCMessage message;

        // MAIN LCDS
        IMyBlockGroup mainLCDGroup;
        List<IMyTerminalBlock> mainSubscribers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> auxMainSubscribers = new List<IMyTerminalBlock>();
        MainLCDPublisher mainPublisher;

        // LOG LCDS
        IMyBlockGroup logLCDGroup;
        List<IMyTerminalBlock> logSubscribers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> auxLogSubscribers = new List<IMyTerminalBlock>();
        LogLCDPublisher logPublisher;

        // Holds log data temporarily for each run
        STULog tempLog;

        public Program() {
            listener = IGC.RegisterBroadcastListener(LIGMA_VEHICLE_BROADCASTER_CHANNEL);

            try {
                mainLCDGroup = GridTerminalSystem.GetBlockGroupWithName(LIGMA_MISSION_CONTROL_MAIN_LCDS_GROUP);
                mainLCDGroup.GetBlocks(mainSubscribers);
                logLCDGroup = GridTerminalSystem.GetBlockGroupWithName(LIGMA_MISSION_CONTROL_LOG_LCDS_GROUP);
                logLCDGroup.GetBlocks(logSubscribers);
            } catch {
                Echo($"Error getting main or log lcds");
            }

            try {
                GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(auxMainSubscribers, block => block.CustomData.Contains(LIGMA_MISSION_CONTROL_AUX_MAIN_LCD_TAG));
                GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(auxLogSubscribers, block => block.CustomData.Contains(LIGMA_MISSION_CONTROL_AUX_LOG_LCD_TAG));
            } catch {
                Echo($"Error gettings main or log aux lcds");
            }

            mainPublisher = new MainLCDPublisher(mainSubscribers, auxMainSubscribers);
            logPublisher = new LogLCDPublisher(logSubscribers, auxLogSubscribers);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {
            Echo($"Last runtime: {Runtime.LastRunTimeMs} ms");

            if (argument == "DETONATE") {
                IGC.SendBroadcastMessage(LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL, "DETONATE", TransmissionDistance.AntennaRelay);
            } else {
                IGC.SendBroadcastMessage(LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL, argument, TransmissionDistance.AntennaRelay);
            }

            while (listener.HasPendingMessage) {
                message = listener.AcceptMessage();
                try {
                    tempLog = STULog.Deserialize(message.Data.ToString());
                    PublishData();
                } catch {
                    tempLog = new STULog {
                        Sender = "LIGMA Missile",
                        Message = $"Received invalid message: {message.Data}",
                        Type = STULogType.ERROR
                    };
                    mainPublisher.UpdateDisplays(tempLog);
                }
            }
        }

        public void PublishData() {
            mainPublisher.UpdateDisplays(tempLog);
            logPublisher.UpdateDisplays(tempLog);
        }
    }
}
