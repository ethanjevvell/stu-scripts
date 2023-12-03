
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public class MainLCDPublisher {

            public List<MainLCD> Displays { get; set; }

            public MainLCDPublisher(List<IMyTerminalBlock> mainSubscribers, List<IMyTerminalBlock> auxSubscribers) {

                Displays = new List<MainLCD>();

                foreach (IMyTerminalBlock subscriber in mainSubscribers) {
                    if (subscriber == null)
                        continue;
                    Displays.Add(new MainLCD(subscriber, 0, "Monospace", 0.7f));
                }

                foreach (IMyTerminalBlock subscriber in auxSubscribers) {
                    if (subscriber == null)
                        continue;
                    int displayIndex = int.Parse(subscriber.CustomData.Split(':')[1]);
                    Displays.Add(new MainLCD(subscriber, displayIndex, "Monospace", 0.4f));
                }

            }

            public void UpdateDisplays(STULog newLog) {
                foreach (MainLCD display in Displays) {
                    display.UpdateDisplay(newLog);
                }
            }

        }
    }
}
