using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public partial class StandardOutput : STUDisplay {

                STUDisplayDrawMapper DrawMapper;

                public StandardOutput(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {
                    DrawMapper = new STUDisplayDrawMapper {
                        DisplayDrawMapper = {
                            { STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.SmallBlockCockpit, STUSubDisplay.LargeDisplay), SmallBuggyCockpit.ScreenArea}
                        }
                    };
                }

                public void DrawTelemetry(double altitude) {
                    StartFrame();
                    MySprite sprite = new MySprite() {
                        Type = SpriteType.TEXT,
                        Data = $"{altitude}",
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
