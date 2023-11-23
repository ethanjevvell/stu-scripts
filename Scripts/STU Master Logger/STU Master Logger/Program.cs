using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        private string MASTER_LOGGER_CHANNEL = "LHQ_MASTER_LOGGER";

        IMyBroadcastListener listener;
        MyIGCMessage message;
        IMyBlockGroup subscribers;
        LogPublisher publisher;

        public Program()
        {
            listener = IGC.RegisterBroadcastListener(MASTER_LOGGER_CHANNEL);
            subscribers = GridTerminalSystem.GetBlockGroupWithName("MASTER_LOGGER_LCDS");

            publisher = new LogPublisher(subscribers);
            publisher.ClearPanels();

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        {
            if (listener.HasPendingMessage)
            {
                message = listener.AcceptMessage();
                StuLog newLog = StuLog.Deserialize(message.Data.ToString());
                if (newLog != null)
                {
                    publisher.Publish(newLog);
                }
                else
                {
                    Echo("Received malformed log");
                }
            }
        }

    }
}
