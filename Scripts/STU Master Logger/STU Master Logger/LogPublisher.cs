using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public class LogPublisher {

            public List<LogLCD> Displays { get; set; }

            public LogPublisher(List<IMyTerminalBlock> subscribers) {
                Displays = new List<LogLCD>();
                foreach (IMyTextSurfaceProvider subscriber in subscribers) {
                    Displays.Add(new LogLCD(subscriber.GetSurface(0), "Monospace", 0.7f));
                }
            }

            public void Publish(STULog newLog) {
                foreach (LogLCD display in Displays) {
                    display.Logs.Enqueue(newLog);
                    display.StartFrame();
                    display.DrawLogs();
                    display.EndAndPaintFrame();
                }
            }

        }
    }
}
