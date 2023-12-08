using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL = "LIGMA_MISSION_CONTROL";
        private const string LIGMA_VEHICLE_BROADCASTER_CHANNEL = "LIGMA_VEHICLE_CONTROL";

        private string command = "";

        LIGMA missile;
        MissileReadout display;
        STUMasterLogBroadcaster broadcaster;
        IMyBroadcastListener listener;
        Phase phase;

        enum Phase {
            Idle,
            Launch,
            Flight,
            Terminal,
            Impact
        }

        public Program() {
            broadcaster = new STUMasterLogBroadcaster(LIGMA_VEHICLE_BROADCASTER_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            listener = IGC.RegisterBroadcastListener(LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL);
            missile = new LIGMA(broadcaster, GridTerminalSystem, Me, Runtime);
            display = new MissileReadout(Me, 0, missile);
            phase = Phase.Idle;

            // Script updates every 100 ticks (roughly 1.67 seconds)
            // IMPORTANT: This must match Mission Control update frequency
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {

            if (!string.IsNullOrEmpty(argument)) {
                if (argument == "DETONATE") {
                    LIGMA.SelfDestruct();
                } else if (argument == "LAUNCH") {
                    phase = Phase.Launch;
                } else {
                    throw new Exception("Invalid command");
                }
            }

            if (listener.HasPendingMessage) {
                var message = listener.AcceptMessage();
                if (message.Data.ToString() == "DETONATE") {
                    LIGMA.SelfDestruct();
                } else {
                    command = message.Data.ToString();
                }
            }

            LIGMA.UpdateMeasurements();
            LIGMA.PingMissionControl();

            switch (phase) {

                case Phase.Idle:
                    if (command == "LAUNCH") {
                        phase = Phase.Launch;
                    }
                    break;

                case Phase.Launch:
                    LIGMA.Launch.Run();
                    break;

                case Phase.Flight:
                    // TODO
                    break;

                case Phase.Terminal:
                    // TODO
                    break;

                case Phase.Impact:
                    // TODO
                    break;

            }

        }
    }
}
