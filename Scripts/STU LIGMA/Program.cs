using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL = "LIGMA_MISSION_CONTROL";
        private const string LIGMA_VEHICLE_BROADCASTER_CHANNEL = "LIGMA_VEHICLE_CONTROL";

        private string command = "";

        Missile missile;
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
            missile = new Missile(broadcaster, GridTerminalSystem, Me, Runtime);
            display = new MissileReadout(Me, 0, missile);
            phase = Phase.Idle;

            // Script updates every 100 ticks (roughly 1.67 seconds)
            // IMPORTANT: This must match Mission Control update frequency
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {


            if (listener.HasPendingMessage) {
                var message = listener.AcceptMessage();
                if (message.Data.ToString() == "DETONATE") {
                    Missile.SelfDestruct();
                } else {
                    command = message.Data.ToString();
                }
            }

            Missile.UpdateMeasurements();
            Missile.PingMissionControl();

            switch (phase) {

                case Phase.Idle:
                    if (command == "LAUNCH") {
                        phase = Phase.Launch;
                    }
                    break;

                case Phase.Launch:
                    Missile.Launch.Run();
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
