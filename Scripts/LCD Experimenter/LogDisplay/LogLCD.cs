using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public partial class LogLCD : STUDisplay {

            public Queue<STULog> Logs = new Queue<STULog>();

            public LogLCD(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {
                Logs.Enqueue(new STULog() {
                    Sender = "Test Sender",
                    Message = "Test Message",
                    Type = STULogType.INFO
                });
                Logs.Enqueue(new STULog() {
                    Sender = "Test sender",
                    Message = "This is a really long long string that should get wrapped on smaller displays but not on the larger displays",
                    Type = STULogType.INFO
                });
            }

        }
    }
}
