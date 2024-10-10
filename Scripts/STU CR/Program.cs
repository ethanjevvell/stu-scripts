using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    partial class Program : MyGridProgram {
        bool Canary = true;

        CR ThisCR;
        STUMasterLogBroadcaster Broadcaster;
        IMyBroadcastListener Listener;
        MyCommandLine CommandLineParser = new MyCommandLine();
        TEA Modem = new TEA();

        public Program() {
            Broadcaster = new STUMasterLogBroadcaster(CBT_VARIABLES.CBT_BROADCAST_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            Listener = IGC.RegisterBroadcastListener(CBT_VARIABLES.CBT_BROADCAST_CHANNEL);
            ThisCR = new CR(Echo, Broadcaster, GridTerminalSystem, Me, Runtime);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource) {
            try {
                if (Listener.HasPendingMessage) {
                    var rawMessage = Listener.AcceptMessage();
                    string message = rawMessage.Data.ToString();
                    STULog incomingLog = STULog.Deserialize(message);
                    // string decryptedMessage = Modem.Decrypt(incomingLog.Message, CBT_VARIABLES.TEA_KEY);

                    CR.AddToLogQueue($"Incoming Log: {incomingLog.Message}; metadata: {incomingLog.Metadata}");

                    ParseCommand(incomingLog.Message.ToUpper());
                }
            } catch (Exception e) {
                CR.AddToLogQueue($"Error: {e.Message}... {e.Source} ({e.StackTrace})", STULogType.ERROR);
            } finally {
                CR.UpdateLogScreens();
            }

        }

        public void ParseCommand(string arg) // arg = "CBT REPORT LOCATION PB"
        {
            if (CommandLineParser.TryParse(arg)) {
                switch (CommandLineParser.Argument(0)) {
                    case "REPORT":
                        Echo($"REPORT request received from {CommandLineParser.Argument(0)}");
                        ProcessReportRequest(AdditionalArguments(CommandLineParser));
                        break;
                    case "TEST":
                        Echo($"TEST command received from {CommandLineParser.Argument(0)}");
                        break;
                    case "PING":
                        Echo($"PING received from {CommandLineParser.Argument(0)}");
                        CR.CreateBroadcast($"{CommandLineParser.Argument(0)}", false, STULogType.INFO);
                        break;
                    default:
                        Echo($"Unknown command received from {CommandLineParser.Argument(0)}");
                        break;
                }
            }
        }

        public string AdditionalArguments(MyCommandLine thisCommandLineParser) {
            string subcommand = "";
            if (thisCommandLineParser.ArgumentCount > 2) {
                for (int i = 2; i < thisCommandLineParser.ArgumentCount; i++) {
                    subcommand = $"{thisCommandLineParser.Argument(i)},";
                }
            }
            return subcommand;
        }

        public void ProcessReportRequest(string arg) {
            string[] report = arg.Split(',');
            string reportType = report[0];
            string reportData = report[1];

            switch (reportType) {
                case "PING":
                    Echo($"PING REPORT received from {CommandLineParser.Argument(0)}");
                    break;
                case "POSITION":
                    Echo($"POSITION REPORT request received from {CommandLineParser.Argument(0)}");
                    break;
                default:
                    Echo($"Unknown report received from {CommandLineParser.Argument(0)}");
                    break;
            }
        }
    }
}
