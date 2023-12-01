using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class MainDisplay {

            public class LargeProgrammableBlockSprites {
                public static void LargeDisplay(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(-117f, -82f) * scale + centerPos,
                        Size = new Vector2(200f, 100f) * scale,
                        Color = new Color(255, 255, 255, 255),
                        RotationOrScale = 0f
                    }); // sprite1
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = Velocity.ToString(),
                        Position = new Vector2(158f, 64f) * scale + centerPos,
                        Color = new Color(255, 255, 255, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // text1
                }

            }

            public class SmallProgrammableBlockSprites {
                public static void LargeDisplay(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(-117f, -82f) * scale + centerPos,
                        Size = new Vector2(200f, 100f) * scale,
                        Color = new Color(255, 255, 255, 255),
                        RotationOrScale = 0f
                    }); // sprite1
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = Velocity.ToString(),
                        Position = new Vector2(158f, 64f) * scale + centerPos,
                        Color = new Color(255, 255, 255, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // text1
                }
            }
        }
    }
}
