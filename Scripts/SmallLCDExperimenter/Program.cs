using Sandbox.ModAPI.Ingame;
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

            LogLCD lcd = new LogLCD(surface, "Monospace", 0.7f);
            Echo(lcd.Lines.ToString());
            Echo(lcd.LineHeight.ToString());
            Echo(lcd.ScreenHeight.ToString());
            Echo(lcd.ScreenWidth.ToString());
            Echo(lcd.Viewport.ToString());

            lcd.StartFrame();
            lcd.DrawLineOfText("Hello World!");
            lcd.DrawLineOfText("Second line!");
            lcd.EndAndPaintFrame();
        }

        public class LogLCD : STUDisplay {

            public LogLCD(IMyTextSurface surface, string font = "Monospace", float fontSize = 1f) : base(surface, font, fontSize) { }

            public void DrawLineOfText(string text) {

                var sprite = new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Position = Viewport.Position,
                    RotationOrScale = Surface.FontSize,
                    Color = Color.White,
                    FontId = Surface.Font,
                };

                CurrentFrame.Add(sprite);
                GoToNextLine();
            }

        }

    }

}
