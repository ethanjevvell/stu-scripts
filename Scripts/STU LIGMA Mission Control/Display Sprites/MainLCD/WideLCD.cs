using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class MainLCD {
            public static class LargeWideLCD {
                public static void ScreenArea(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = $"Velocity: {Velocity}",
                        Position = new Vector2(0f, 0f) * scale + centerPos,
                        Color = new Color(255, 255, 255, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // text1
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = $"Fuel: {CurrentFuel}",
                        Position = new Vector2(0f, 32f) * scale + centerPos,
                        Color = new Color(255, 255, 255, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // text2
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = $"Power: {CurrentPower}",
                        Position = new Vector2(0f, 68f) * scale + centerPos,
                        Color = new Color(255, 255, 255, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // text3
                }
            }
        }
    }
}
