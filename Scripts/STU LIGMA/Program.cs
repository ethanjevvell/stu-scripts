using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_BROADCAST_CHANNEL = "LIGMA_MISSION_CONTROL";

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

            broadcaster.Log(new STULog {
                Sender = "LIGMA-I",
                Message = "PING",
                Type = STULogType.OK,
                Metadata = new Dictionary<string, string> {
                    { "Velocity", "12" },
                    { "CurrentFuel", "1000" },
                    { "CurrentPower", "2000" }
                }
            });

        }


    }
}
