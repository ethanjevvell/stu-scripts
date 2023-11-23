using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public class STUMasterLogBroadcaster
        {
            public string Channel { get; set; }
            public IMyIntergridCommunicationSystem Broadcaster { get; set; }
            public TransmissionDistance Distance { get; set; }

            public STUMasterLogBroadcaster(string channel, IMyIntergridCommunicationSystem IGC, TransmissionDistance distance)
            {
                Channel = channel;
                Broadcaster = IGC;
                Distance = distance;
            }

            public void Log(STULog log)
            {
                Broadcaster.SendBroadcastMessage(Channel, log.Serialize(), Distance);
            }

        }
    }
}
