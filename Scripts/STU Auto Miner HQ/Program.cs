
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyBroadcastListener LIGMAListener;
        IMyBroadcastListener ReconListener;
        MyIGCMessage message;

        // MAIN LCDS
        IMyBlockGroup mainLCDGroup;
        List<IMyTerminalBlock> mainSubscribers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> auxMainSubscribers = new List<IMyTerminalBlock>();
        MainLCDPublisher mainPublisher;

        // LOG LCDS
        IMyBlockGroup logLCDGroup;
        List<IMyTerminalBlock> logSubscribers = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> auxLogSubscribers = new List<IMyTerminalBlock>();
        LogLCDPublisher logPublisher;
        StringBuilder telemetryRecords = new StringBuilder();

        // Holds log data temporarily for each run
        STULog IncomingLog;
        STULog OutgoingLog;
        Dictionary<string, string> tempMetadata;

        public Program() {

        }

        public void Main(string argument) {

        }

    }
}
