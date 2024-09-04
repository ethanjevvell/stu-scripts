using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public partial class StandardOutput : STUDisplay {
                public StandardOutput(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {

                }

                public void DrawTelemetry() {
                    StartFrame();
                    MySprite sprite = new MySprite() {
                        Type = SpriteType.TEXT,
                        Data = Telemetry["CurrentVelocity"],
                        Position = Center,
                        RotationOrScale = 1f,
                        Color = new Color(255, 255, 255),
                        FontId = "Monospace"
                    };
                    CurrentFrame.Add(sprite);
                    EndAndPaintFrame();
                }
            }
        }
    }
}
