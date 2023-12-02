using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {

    partial class Program {

        public partial class MainDisplay {

            public static class LargeProgrammableBlock {
                public static void LargeScreen(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(3f, -110f) * scale + centerPos,
                        Size = new Vector2(512f, 100f) * scale,
                        Color = new Color(0, 0, 128, 255),
                        RotationOrScale = 0f
                    }); // sprite2
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = "STU Planetary Measurements (V 1.0)",
                        Position = new Vector2(-238f, -126f) * scale + centerPos,
                        Color = new Color(255, 255, 255, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // text1
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = $"G_w Vector: ({WorldGravityVector.X.ToString("F2")}, {WorldGravityVector.Y.ToString("F2")}, {WorldGravityVector.Z.ToString("F2")})\n" +
                               $"M_w: {WorldGravityMagniutde.ToString("F2")}\nM_b: {BodyGravityMagnitude.ToString("F2")}",
                        Position = new Vector2(-218f, -30f) * scale + centerPos,
                        Color = new Color(255, 255, 255, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // text2
                }
            }

            public static class SmallProgrammableBlock {

            }
        }
    }
}
