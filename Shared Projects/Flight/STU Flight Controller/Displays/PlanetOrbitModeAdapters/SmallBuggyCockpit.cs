using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class SmallBuggyCockpit {
                public static void ScreenArea(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
                    // TODO: Create if statements that change the output sprite depending on the output mode! 
                    MySprite test = new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareHollow",
                        Position = new Vector2(0f, 0f) * scale + centerPos,
                        Size = new Vector2(1080f, 534f) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }; // sprite6

                    MySprite background = new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(0f, 0f) * scale + centerPos,
                        Color = new Color(0, 0, 0, 255),
                        RotationOrScale = 0f
                    }; // sprite7
                }
            }
        }
    }
}
