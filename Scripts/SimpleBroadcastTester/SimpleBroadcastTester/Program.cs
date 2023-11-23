
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        private string MASTER_LOGGER_CHANNEL = "LHQ_MASTER_LOGGER";
        private int counter = 0;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        {
            // cycle logtype through ok, warning and error based on counter
            STULog.LogType logType = STULog.LogType.OK;
            if (counter % 3 == 0)
            {
                logType = STULog.LogType.WARNING;
            }
            else if (counter % 3 == 1)
            {
                logType = STULog.LogType.ERROR;
            }
            STULog log = new STULog { Message = $"{counter}", Sender = "SMPL_BROADCAST", Type = logType };

            counter++;
            IGC.SendBroadcastMessage(MASTER_LOGGER_CHANNEL, log.Serialize(), TransmissionDistance.CurrentConstruct);
        }
    }
}
