using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class LogLCD : STUDisplay {

            public Queue<STULog> Logs { get; set; }
            private string HeaderText { get; set; }
            private int HeaderLines { get; set; }

            /// <summary>
            /// An LCD display that can draw STULogs.
            /// Extends the STUDisplay class.
            /// Implements line-drawing functionality.
            /// </summary>
            /// <param name="surface"></param>
            /// <param name="font"></param>
            /// <param name="fontSize"></param>

            public LogLCD(IMyTextSurface surface, int headerLines, string headerText, string font = "Monospace", float fontSize = 1f) : base(surface, font, fontSize) {
                Logs = new Queue<STULog>();
                // Override STUDisplay's default background color
                Surface.ScriptBackgroundColor = new Color(48, 10, 36);
                HeaderLines = headerLines;
                HeaderText = headerText;

                var headerSpriteBackground = new MySprite() {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = TopLeft + new Vector2(0, LineHeight * HeaderLines / 2f),
                    Color = new Color(44, 44, 44),
                    Size = new Vector2(ScreenWidth, LineHeight * HeaderLines),
                };

                float headerScale = 1.2f;
                float largerLineHeight = LineHeight * headerScale;
                var headerSpriteText = new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = HeaderText,
                    Position = TopLeft + new Vector2(ScreenWidth / 2f, ((LineHeight * HeaderLines) - largerLineHeight) / 2f),
                    // Make header text slightly larger than the rest of the text
                    RotationOrScale = Surface.FontSize * headerScale,
                    Alignment = TextAlignment.CENTER,
                    Color = Color.White,
                    FontId = Surface.Font,
                };

                BackgroundSprite = new MySpriteCollection {
                    Sprites = new MySprite[] {
                        headerSpriteBackground, headerSpriteText
                    }
                };

            }

            public string FormatLog(STULog log) => $" > {log.Sender}: {log.Message}";

            public void DrawLineOfText(STULog log) {

                var sprite = new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = FormatLog(log),
                    Position = Cursor,
                    RotationOrScale = Surface.FontSize,
                    Color = STULog.GetColor(log.Type),
                    FontId = Surface.Font,
                };

                CurrentFrame.Add(sprite);
                GoToNextLine();
            }

            public void DrawLogs() {
                // Place the cursor at the top left of the log section of the display
                Cursor = TopLeft + new Vector2(0, LineHeight * HeaderLines);

                // Scroll effect implemented with a queue
                if (Logs.Count > Lines - HeaderLines) {
                    Logs.Dequeue();
                }

                foreach (var log in Logs) {
                    DrawLineOfText(log);
                }
            }

        }
    }
}
