using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public partial class MainDisplay {

            public class LargeStandardLCDSprites {
                public static void ScreenArea(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = Velocity.ToString(),
                        Position = new Vector2(-96f, -152f) * scale + centerPos,
                        Color = new Color(255, 255, 255, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // text1
                }

            }
        }
    }
}
