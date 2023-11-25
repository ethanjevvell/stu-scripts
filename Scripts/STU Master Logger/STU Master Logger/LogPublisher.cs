using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public class LogPublisher {

            public List<LogLCD> Displays { get; set; }

            public LogPublisher(List<IMyTerminalBlock> mainSubscribers, List<IMyTerminalBlock> auxSubscribers, string headerText) {
                Displays = new List<LogLCD>();
                foreach (IMyTextSurfaceProvider subscriber in mainSubscribers) {
                    if (subscriber == null)
                        continue;
                    Displays.Add(new LogLCD(subscriber.GetSurface(0), 2, headerText, "Monospace", 0.7f));
                }
                foreach (IMyTerminalBlock subscriber in auxSubscribers) {
                    if (subscriber == null)
                        continue;
                    int index = int.Parse(subscriber.CustomData.Split(':')[1]);
                    var block = subscriber as IMyTextSurfaceProvider;
                    Displays.Add(new LogLCD(block.GetSurface(index), 2, headerText, "Monospace", 0.4f));
                }
            }

            public void Publish(STULog newLog) {
                if (Displays.Count > 0) {
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
}
