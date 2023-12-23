
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_MAIN_LCDS_GROUP = "LIGMA_MISSION_CONTROL_MAIN_LCDS";
        private const string LIGMA_MISSION_CONTROL_LOG_LCDS_GROUP = "LIGMA_MISSION_CONTROL_LOG_LCDS";
        private const string LIGMA_MISSION_CONTROL_AUX_MAIN_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_MAIN_LCD:";
        private const string LIGMA_MISSION_CONTROL_AUX_LOG_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_LOG_LCD:";

        IMyBroadcastListener LIGMAListener;
        IMyBroadcastListener ReconListener;
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

            LIGMAListener = IGC.RegisterBroadcastListener(LIGMA_VARIABLES.LIGMA_VEHICLE_BROADCASTER);
            ReconListener = IGC.RegisterBroadcastListener(LIGMA_VARIABLES.LIGMA_RECONNOITERER_BROADCASTER);

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

            if (!string.IsNullOrEmpty(argument)) {
                IGC.SendBroadcastMessage(LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER, argument, TransmissionDistance.AntennaRelay);
                Echo($"Sending message: {argument}");
            }

            HandleIncomingBroadcasts();
        }

        public void HandleIncomingBroadcasts() {
            HandleIncomingLIGMABroadcasts();
            HandleIncomingReconBroadcasts();
        }

        public void HandleIncomingLIGMABroadcasts() {
            while (LIGMAListener.HasPendingMessage) {
                message = LIGMAListener.AcceptMessage();
                try {
                    tempLog = STULog.Deserialize(message.Data.ToString());
                } catch {
                    tempLog = new STULog {
                        Sender = LIGMA_VARIABLES.LIGMA_VEHICLE_NAME,
                        Message = $"Received invalid message: {message.Data}",
                        Type = STULogType.ERROR
                    };
                }
                PublishData();
            }

        }

        public void HandleIncomingReconBroadcasts() {
            while (ReconListener.HasPendingMessage) {
                message = ReconListener.AcceptMessage();
                try {
                    tempLog = STULog.Deserialize(message.Data.ToString());
                    SendTargetDataToLIGMA();
                } catch {
                    tempLog = new STULog {
                        Sender = LIGMA_VARIABLES.LIGMA_RECONNOITERER_NAME,
                        Message = $"Received invalid message: {tempLog.Serialize()}",
                        Type = STULogType.ERROR
                    };
                }
                logPublisher.UpdateDisplays(tempLog);
            }
        }

        public void SendTargetDataToLIGMA() {
            STULog commandLog = new STULog {
                Sender = LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER,
                Message = "-targetData",
                Type = STULogType.INFO,
                Metadata = tempLog.Metadata
            };
            IGC.SendBroadcastMessage(LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER, commandLog.Serialize(), TransmissionDistance.AntennaRelay);
        }

        public void PublishData() {
            mainPublisher.UpdateDisplays(tempLog);
            logPublisher.UpdateDisplays(tempLog);
        }
    }
}
