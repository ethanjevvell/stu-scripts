using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;

namespace IngameScript {
    partial class Program {
        public class LogLCD : STUDisplay {

            public Queue<STULog> Logs { get; set; }

            /// <summary>
            /// An LCD display that can draw STULogs.
            /// Extends the STUDisplay class.
            /// Implements line-drawing functionality.
            /// </summary>
            /// <param name="surface"></param>
            /// <param name="font"></param>
            /// <param name="fontSize"></param>
            public LogLCD(IMyTextSurface surface, string font = "Monospace", float fontSize = 1f) : base(surface, font, fontSize) {
                Logs = new Queue<STULog>();
            }

            public void DrawLineOfText(ref MySpriteDrawFrame frame, STULog log) {
                var logString = log.GetLogString();

                var sprite = new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = logString,
                    Position = Viewport.Position,
                    RotationOrScale = Surface.FontSize,
                    Color = STULog.GetColor(log.Type),
                    FontId = Surface.Font,
                };

                frame.Add(sprite);
                GoToNextLine();
            }


        }
    }
}
