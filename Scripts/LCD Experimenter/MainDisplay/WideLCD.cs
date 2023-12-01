using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class LargeWideLCDSprites {
            public static void ScreenArea(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
                frame.Add(new MySprite() {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(-46f, 134f) * scale + centerPos,
                    Size = new Vector2(228f, 100f) * scale,
                    Color = new Color(128, 255, 255, 255),
                    RotationOrScale = 0f
                }); // sprite1
            }
        }
        public class SmallWideLCDSprites {

        }
    }
}
