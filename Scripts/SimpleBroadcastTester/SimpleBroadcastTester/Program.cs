﻿
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private string MASTER_LOGGER_CHANNEL = "LHQ_MASTER_LOGGER";
        private STUMasterLogBroadcaster masterLogBroadcaster;
        public int counter = 0;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            masterLogBroadcaster = new STUMasterLogBroadcaster(MASTER_LOGGER_CHANNEL, IGC, TransmissionDistance.CurrentConstruct);
        }

        public void Main() {
            counter++;
            STULog log = new STULog("TEST", $"Successful test - {counter}", STULogType.OK);
            masterLogBroadcaster.Log(log);
        }
    }
}
