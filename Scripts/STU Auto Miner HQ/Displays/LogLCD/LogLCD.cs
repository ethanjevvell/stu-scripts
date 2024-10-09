using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public partial class LogLCD : STUDisplay {

            public Queue<STULog> FlightLogs { get; set; }


            public LogLCD(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {
                FlightLogs = new Queue<STULog>();
            }

            public void Update() {
                StartFrame();
                WriteWrappableLogs(FlightLogs);
                EndAndPaintFrame();
            }

        }
    }
}