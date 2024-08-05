using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;

namespace IngameScript {
    partial class Program {
        public class LogLCDPublisher {

            public List<LogLCD> Displays { get; set; }

            public LogLCDPublisher(List<IMyTerminalBlock> mainSubscribers, List<IMyTerminalBlock> auxSubscribers) {

                Displays = new List<LogLCD>();

                foreach (IMyTerminalBlock subscriber in mainSubscribers) {
                    if (subscriber == null)
                        continue;
                    Displays.Add(new LogLCD(subscriber, 0, "Monospace", 0.7f));
                }

                foreach (IMyTerminalBlock subscriber in auxSubscribers) {
                    if (subscriber == null)
                        continue;
                    int displayIndex = int.Parse(subscriber.CustomData.Split(':')[1]);
                    Displays.Add(new LogLCD(subscriber, displayIndex, "Monospace", 0.4f));
                }

            }

            public void UpdateDisplays(STULog newLog) {
                try {
                    foreach (LogLCD display in Displays) {
                        // Only STULogs with a Message property should be displayed
                        // Anything else is just telemetry
                        if (!string.IsNullOrEmpty(newLog.Message)) {
                            display.FlightLogs.Enqueue(newLog);
                            display.StartFrame();
                            display.WriteWrappableLogs(display.FlightLogs);
                            display.EndAndPaintFrame();
                            //display.UpdateDisplay();
                        }
                    }
                } catch (System.Exception e) {
                    foreach (LogLCD display in Displays) {
                        display.StartFrame();
                        display.CurrentFrame.Add(new MySprite() {
                            Type = SpriteType.TEXT,
                            Data = e.Message,
                            Position = display.TopLeft,
                            RotationOrScale = display.Surface.FontSize * 0.75f,
                            Color = STULog.GetColor(STULogType.INFO),
                            FontId = display.Surface.Font
                        });
                        display.EndAndPaintFrame();
                    }
                }
            }

        }
    }
}
