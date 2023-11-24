using Sandbox.ModAPI.Ingame;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        public Program() {
        }

        public void Main() {
            var block = GridTerminalSystem.GetBlockWithName("TEST_CONTROL_SEAT") as IMyTextSurfaceProvider;
            var surface = block.GetSurface(0);
            Echo(surface.DisplayName);

            LogLCD lcd = new LogLCD(surface);
            Echo(lcd.Lines.ToString());
            Echo(lcd.LineHeight.ToString());
            Echo(lcd.ScreenHeight.ToString());
            Echo(lcd.ScreenWidth.ToString());
            Echo(lcd.Viewport.ToString());

            var frame = lcd.Surface.DrawFrame();
            lcd.DrawLineOfText(ref frame, "Hello World!");
            frame.Dispose();
        }

        public class LogLCD {
            public IMyTextSurface Surface { get; set; }
            public RectangleF Viewport { get; set; }
            public float ScreenWidth { get; set; }
            public float ScreenHeight { get; set; }
            public float LineHeight { get; set; }
            public int Lines { get; set; }

            /// <summary>
            /// A wrapper for a text surface that allows for easy drawing of logs.
            /// Initializes an LCD with the given font and font size.
            /// If not specified, the default font is Monospace and the default font size is 1.
            /// </summary>
            /// <param name="surface"></param>
            /// <param name="font"></param>
            /// <param name="fontSize"></param>
            public LogLCD(IMyTextSurface surface, string font = "Monospace", float fontSize = 1f) {
                Surface = surface;
                Surface.ContentType = ContentType.SCRIPT;
                Surface.ScriptBackgroundColor = Color.Black;
                Surface.FontSize = fontSize;
                Surface.Font = font;
                Viewport = new RectangleF((Surface.TextureSize - Surface.SurfaceSize) / 2f, Surface.SurfaceSize);
                ScreenWidth = Viewport.Width;
                ScreenHeight = Viewport.Height;
                LineHeight = CalculateLineHeight();
                Lines = (int)(ScreenHeight / LineHeight);
            }

            public void GoToNextLine() {
                Viewport = new RectangleF(
                    new Vector2(Viewport.Position.X, Viewport.Position.Y + LineHeight),
                    Viewport.Size);
            }

            private float CalculateLineHeight() {
                StringBuilder sb = new StringBuilder("E");
                Vector2 stringDimensions = Surface.MeasureStringInPixels(sb, Surface.Font, Surface.FontSize);
                return stringDimensions.Y;
            }

            public void DrawLineOfText(ref MySpriteDrawFrame frame, string text) {

                var sprite = new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Position = Viewport.Position,
                    RotationOrScale = Surface.FontSize,
                    Color = Color.White,
                    FontId = Surface.Font,
                };

                frame.Add(sprite);
                GoToNextLine();
            }


        }

    }

}
