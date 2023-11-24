using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

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

            public void ClearPanels() {
                foreach (LogLCD display in Displays) {
                    display.Surface.WriteText("");
                }
            }

            public void DrawLogs(ref MySpriteDrawFrame frame, LogLCD display) {
                display.Viewport = new RectangleF(new Vector2(0, 0), display.Viewport.Size);

                // Scroll effect implemented with a queue
                if (display.Logs.Count > display.Lines) {
                    display.Logs.Dequeue();
                }

                foreach (var log in display.Logs) {
                    display.DrawLineOfText(ref frame, log);
                }
                frame.Dispose();
            }

            public void Publish(STULog newLog) {
                foreach (LogLCD display in Displays) {
                    display.Logs.Enqueue(newLog);
                    var frame = display.Surface.DrawFrame();
                    DrawLogs(ref frame, display);
                    frame.Dispose();
                }
            }

        }
    }
}
