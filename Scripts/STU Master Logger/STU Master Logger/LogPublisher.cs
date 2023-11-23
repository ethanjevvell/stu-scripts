using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class LogPublisher
        {
            List<IMyTerminalBlock> panels = new List<IMyTerminalBlock>();
            List<LogLCD> displays = new List<LogLCD>();
            IMyBlockGroup subscribers;

            public LogPublisher(IMyBlockGroup subscribers)
            {
                this.subscribers = subscribers;
                this.subscribers.GetBlocks(panels);
                foreach (IMyTextSurfaceProvider panel in panels)
                {
                    displays.Add(new LogLCD(panel.GetSurface(0)));
                }
            }

            public void ClearPanels()
            {
                foreach (IMyTextSurface panel in panels)
                {
                    panel.WriteText("");
                }
            }

            public void DrawLineOfText(ref MySpriteDrawFrame frame, LogLCD display, STULog log)
            {
                var logString = log.GetLogString();

                var sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = logString,
                    Position = display.Viewport.Position,
                    RotationOrScale = display.Surface.FontSize,
                    Color = STULog.GetColor(log.Type),
                    FontId = display.Surface.Font,
                };

                frame.Add(sprite);
            }


            public void DrawLogs(ref MySpriteDrawFrame frame, LogLCD display)
            {
                display.Viewport.Position = new Vector2(0, 0);

                // Scroll effect implemented with a queue
                if (display.Logs.Count > display.Lines)
                {
                    display.Logs.Dequeue();
                }

                foreach (var log in display.Logs)
                {
                    DrawLineOfText(ref frame, display, log);
                    display.GoToNextLine();
                }
                frame.Dispose();
            }

            public void Publish(STULog newLog)
            {
                foreach (LogLCD display in displays)
                {
                    display.Logs.Enqueue(newLog);
                    var frame = display.Surface.DrawFrame();
                    DrawLogs(ref frame, display);
                    frame.Dispose();
                }
            }

        }
    }
}
