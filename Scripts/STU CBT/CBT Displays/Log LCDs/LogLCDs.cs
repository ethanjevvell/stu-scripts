using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class LogLCDs : STUDisplay
        {
            public Queue<STULog> FlightLogs;

            public LogLCDs(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize)
            {
                FlightLogs = new Queue<STULog>();
            }

            public string FormatLog(STULog log) => $" > {log.Sender}: {log.Message} ";

            private void DrawLogs()
            {
                Cursor = TopLeft + new Vector2(0, 5);

                // Scroll effect implemented with a queue
                if (FlightLogs.Count > Lines)
                {
                    FlightLogs.Dequeue();
                }

                // Draw the logs
                foreach (var log in FlightLogs)
                {
                    DrawLineOfText(log);
                }
            }

            public void DrawLineOfText(STULog log)
            {
                var sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = FormatLog(log),
                    Position = Cursor,
                    RotationOrScale = Surface.FontSize * 0.75f,
                    Color = STULog.GetColor(log.Type),
                    FontId = Surface.Font,
                };

                CurrentFrame.Add(sprite);
                GoToNextLine();
            }

            public void UpdateDisplay()
            {
                StartFrame();
                DrawLogs();
                EndAndPaintFrame();
            }
        }
    }
}
