﻿
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        private string MASTER_LOGGER_CHANNEL = "LHQ_MASTER_LOGGER";
        private STUMasterLogBroadcaster masterLogBroadcaster;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            masterLogBroadcaster = new STUMasterLogBroadcaster(MASTER_LOGGER_CHANNEL, IGC, TransmissionDistance.CurrentConstruct);
        }

        public void Main()
        {
            STULog log = new STULog("TEST_SENDER", "This is a test log message", STULog.LogType.OK);
            masterLogBroadcaster.Log(log);
        }
    }
}
