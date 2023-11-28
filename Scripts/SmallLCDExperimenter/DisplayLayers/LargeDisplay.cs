using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public partial class TestDisplay {

            public static void DrawStandardLCDSprites(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
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

            public static void DrawWideLCDSprites(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
                frame.Add(new MySprite() {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = "Text",
                    Position = new Vector2(394f, 172f) * scale + centerPos,
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Debug",
                    RotationOrScale = 1f * scale
                }); // text1
            }

        }
    }
}
