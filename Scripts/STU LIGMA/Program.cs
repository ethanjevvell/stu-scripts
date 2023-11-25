using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_BROADCAST_CHANNEL = "LIGMA";

        Missile missile;
        MissileReadout display;
        STUMasterLogBroadcaster broadcaster;


        public Program() {

            broadcaster = new STUMasterLogBroadcaster(LIGMA_BROADCAST_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            missile = new Missile(broadcaster, GridTerminalSystem, Me);
            display = new MissileReadout(Me.GetSurface(0), missile);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main() {

        }


    }
}
