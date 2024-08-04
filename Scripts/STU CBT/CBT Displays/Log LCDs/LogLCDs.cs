using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
            public int MaxCharsPerLine;
            public static Action<string> echo;

            public LogLCDs(Action<string> Echo, IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize)
            {
                echo = Echo;
                FlightLogs = new Queue<STULog>();
                StringBuilder builder = new StringBuilder();
                MaxCharsPerLine = (int)Math.Floor(Viewport.Width / Surface.MeasureStringInPixels(builder.Append("A"), "Monospace", fontSize).X);
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

                // Draw the logs, splitting them into multiple lines if necessary
                foreach (var log in FlightLogs)
                {
                    string formattedLog = FormatLog(log);
                    for (int i = 0; i * MaxCharsPerLine < formattedLog.Length; i++)
                    {
                        echo($"i = {i}");
                        string output = formattedLog.Substring(i * MaxCharsPerLine, Math.Min(MaxCharsPerLine,formattedLog.Length - i));
                        DrawLineOfText(output, STULog.GetColor(log.Type));
                    }
                }
            }

            public void DrawLineOfText(string text, Color color)
            {
                var sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Position = Cursor,
                    RotationOrScale = Surface.FontSize * 0.75f,
                    Color = color,
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
