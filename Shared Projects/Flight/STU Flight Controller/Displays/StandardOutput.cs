using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public partial class StandardOutput : STUDisplay {

                //STUDisplayDrawMapper DrawMapper;

                public StandardOutput(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {
                    //DrawMapper = new STUDisplayDrawMapper {
                    //    DisplayDrawMapper = {
                    //        { STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.SmallBlockCockpit, STUSubDisplay.LargeDisplay), SmallBuggyCockpit.ScreenArea}
                    //    }
                    //};
                }

                public void DrawTelemetry() {
                    StartFrame();
                    MySprite textSprite = new MySprite() {
                        Type = SpriteType.TEXT,
                        Data = $"ALTITUDE HERE",
                        RotationOrScale = 1f,
                        Size = new Vector2(Viewport.Width / 2, Viewport.Height / 2),
                        Position = TopLeft,
                        Color = Color.White,
                        FontId = "Monospace"
                    };
                    CurrentFrame.Add(textSprite);
                    EndAndPaintFrame();
                }
            }
        }
    }
}
