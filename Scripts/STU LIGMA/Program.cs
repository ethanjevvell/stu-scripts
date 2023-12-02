using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_BROADCAST_CHANNEL = "LIGMA_MISSION_CONTROL";

        Missile missile;
        MissileReadout display;
        STUMasterLogBroadcaster broadcaster;

        public Program() {
            broadcaster = new STUMasterLogBroadcaster(LIGMA_BROADCAST_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            Echo("Broadcaster initiated");
            missile = new Missile(broadcaster, GridTerminalSystem, Me, Runtime);
            Echo("Missile initiated");
            display = new MissileReadout(Me, 0, missile);
            Echo("display done");

            // Script updates every 100 ticks (roughly 1.67 seconds)
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main() {

            missile.PingMissionControl();

        }


    }
}
