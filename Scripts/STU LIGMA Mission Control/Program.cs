
using Sandbox.ModAPI.Ingame;
using System;
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
        STULog IncomingLog;
        STULog OutgoingLog;

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
            OutgoingLog = new STULog {
                Sender = LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER,
                Message = argument,
                Type = STULogType.INFO
            };

            if (!string.IsNullOrEmpty(argument)) {
                IGC.SendBroadcastMessage(LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER, OutgoingLog.Serialize(), TransmissionDistance.AntennaRelay);
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
                    IncomingLog = STULog.Deserialize(message.Data.ToString());
                } catch (Exception e) {
                    IncomingLog = new STULog {
                        Sender = LIGMA_VARIABLES.LIGMA_VEHICLE_NAME,
                        Message = $"Received invalid message: {message.Data}",
                        Type = STULogType.ERROR
                    };
                    IncomingLog.Message = e.ToString();
                }
                PublishData();
            }

        }

        public void HandleIncomingReconBroadcasts() {
            while (ReconListener.HasPendingMessage) {
                message = ReconListener.AcceptMessage();
                try {
                    IncomingLog = STULog.Deserialize(message.Data.ToString());
                    SendTargetDataToLIGMA();
                } catch {
                    IncomingLog = new STULog {
                        Sender = LIGMA_VARIABLES.LIGMA_RECONNOITERER_NAME,
                        Message = $"Received invalid message: {IncomingLog.Serialize()}",
                        Type = STULogType.ERROR
                    };
                }
                logPublisher.UpdateDisplays(IncomingLog);
            }
        }

        public void SendTargetDataToLIGMA() {
            STULog commandLog = new STULog {
                Sender = LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER,
                Message = LIGMA_VARIABLES.COMMANDS.UpdateTargetData.ToString(),
                Type = STULogType.INFO,
                Metadata = IncomingLog.Metadata
            };
            IGC.SendBroadcastMessage(LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER, commandLog.Serialize(), TransmissionDistance.AntennaRelay);
        }

        public void PublishData() {
            mainPublisher.UpdateDisplays(IncomingLog);
            logPublisher.UpdateDisplays(IncomingLog);
        }
    }
}
