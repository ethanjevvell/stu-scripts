
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

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
            try {
                Dictionary<string, string> testMetadata = new Dictionary<string, string> {
                    { "First Arg", "Second Val" },
                    { "SecondArg", "SecondVal" }
                };
                STULog log = new STULog("TEST", $"Successful test - {counter}", STULogType.OK, testMetadata);
                masterLogBroadcaster.Log(log);
            } catch (System.Exception e) {
                Echo(e.StackTrace);
            }
        }
    }
}
