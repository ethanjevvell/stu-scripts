
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public class LogPublisher {

            public List<MissionControlLCD> Displays { get; set; }

            public LogPublisher(List<IMyTerminalBlock> mainSubscribers, List<IMyTerminalBlock> auxSubscribers) {

                Displays = new List<MissionControlLCD>();

                foreach (IMyTextSurfaceProvider subscriber in mainSubscribers) {
                    if (subscriber == null)
                        continue;
                    Displays.Add(new MissionControlLCD(subscriber.GetSurface(0), "Monospace", 0.7f));
                }

                foreach (IMyTerminalBlock subscriber in auxSubscribers) {
                    if (subscriber == null)
                        continue;
                    int index = int.Parse(subscriber.CustomData.Split(':')[1]);
                    var block = subscriber as IMyTextSurfaceProvider;
                    Displays.Add(new MissionControlLCD(block.GetSurface(index), "Monospace", 0.4f));
                }

            }

            public void PublishLog(STULog newLog) {
                foreach (MissionControlLCD display in Displays) {
                    display.FlightLogs.Enqueue(newLog);
                    display.StartFrame();
                    display.DrawLogs(newLog);
                    display.EndAndPaintFrame();
                }
            }

        }
    }
}
