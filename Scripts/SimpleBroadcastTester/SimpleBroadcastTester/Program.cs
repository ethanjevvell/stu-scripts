
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
            StuLog log = new StuLog { Message = $"Count: {counter}", Sender = "pb" };
            string serializedLog = log.Serialize();

            counter++;
            IGC.SendBroadcastMessage(MASTER_LOGGER_CHANNEL, serializedLog, TransmissionDistance.CurrentConstruct);
        }
    }
}
