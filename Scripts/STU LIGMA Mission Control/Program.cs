
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_MAIN_LCDS_GROUP = "LIGMA_MISSION_CONTROL_MAIN_LCDS";
        private const string LIGMA_MISSION_CONTROL_LOG_LCDS_GROUP = "LIGMA_MISSION_CONTROL_LOG_LCDS";
        private const string LIGMA_MISSION_CONTROL_AUX_MAIN_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_MAIN_LCD:";
        private const string LIGMA_MISSION_CONTROL_AUX_LOG_LCD_TAG = "LIGMA_MISSION_CONTROL_AUX_LOG_LCD:";

        private const string telemetryRecordHeader = "Timestamp, Phase, V_x, V_y, V_z, A_x, A_y, A_z, Fuel, Power\n";

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
        StringBuilder telemetryRecords = new StringBuilder();

        // Holds log data temporarily for each run
        STULog IncomingLog;
        STULog OutgoingLog;
        Dictionary<string, string> tempMetadata;

        int TELEMETRY_WRITE_COUNTER = 0;
        int TELEMETRY_WRITE_INTERVAL = 15;

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

            // Write telemetry to CustomData every TELEMETRY_WRITE_INTERVAL runs
            if (TELEMETRY_WRITE_COUNTER >= TELEMETRY_WRITE_INTERVAL) {
                Me.CustomData = telemetryRecords.ToString();
                TELEMETRY_WRITE_COUNTER = 0;
            } else {
                TELEMETRY_WRITE_COUNTER++;
            }
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
                } catch {
                    IncomingLog = new STULog {
                        Sender = LIGMA_VARIABLES.LIGMA_VEHICLE_NAME,
                        Message = $"Received invalid message: {message.Data}",
                        Type = STULogType.ERROR
                    };
                }
                PublishData();
                if (IncomingLog.Metadata != null && IncomingLog.Metadata["Phase"] != "Idle") {
                    WriteTelemetry();
                }
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
                Message = LIGMA_VARIABLES.COMMANDS.UpdateTargetData,
                Type = STULogType.INFO,
                Metadata = IncomingLog.Metadata
            };
            IGC.SendBroadcastMessage(LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER, commandLog.Serialize(), TransmissionDistance.AntennaRelay);
        }

        public void PublishData() {
            mainPublisher.UpdateDisplays(IncomingLog);
            logPublisher.UpdateDisplays(IncomingLog);
        }

        //private const string telemetryRecordHeader = "Timestamp, Phase, V_x, V_y, V_z, A_x, A_y, A_z, Fuel, Power\n";
        public void WriteTelemetry() {
            tempMetadata = IncomingLog.Metadata;
            Vector3D parsedVelocity;
            Vector3D parsedAcceleration;
            parsedVelocity = Vector3D.TryParse(tempMetadata["VelocityComponents"], out parsedVelocity) ? parsedVelocity : Vector3D.Zero;
            parsedAcceleration = Vector3D.TryParse(tempMetadata["AccelerationComponents"], out parsedAcceleration) ? parsedAcceleration : Vector3D.Zero;
            telemetryRecords.Append($"{tempMetadata["Timestamp"]}, {tempMetadata["Phase"]}, {parsedVelocity.X}, {parsedVelocity.Y}, {parsedVelocity.Z}, {parsedAcceleration.X}, {parsedAcceleration.Y}, {parsedAcceleration.Z}, {tempMetadata["CurrentFuel"]}, {tempMetadata["CurrentPower"]}\n");
        }
    }
}
