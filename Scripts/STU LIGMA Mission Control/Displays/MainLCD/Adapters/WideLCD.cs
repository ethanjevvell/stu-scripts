using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class MainLCD {
            public static class LargeWideLCD {
                public static void ScreenArea(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {

                    double fuelFilledRatio = CurrentFuel / FuelCapacity;
                    double powerStoredRatio = CurrentPower / PowerCapacity;
                    string velocityFuelPowerString = $"Velocity abs: {VelocityMagnitude}\nVelocity components: \n{VelocityComponents.X.ToString("F2")}, \n{VelocityComponents.Y.ToString("F2")},\n {VelocityComponents.Z.ToString("F2")}\nFuel: {(int)(fuelFilledRatio * 100)}%\nPower: {(int)(CurrentPower / PowerCapacity) * 100}%";

                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = "LIGMA MK-I",
                        Position = new Vector2(-480f, -232f) * scale + centerPos,
                        Color = new Color(0, 255, 0, 255),
                        FontId = "Debug",
                        RotationOrScale = 2f * scale
                    }); // text1
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(192f, 46f) * scale + centerPos,
                        Size = new Vector2(30f, 270f) * scale,
                        Color = new Color(192, 192, 192, 255),
                        RotationOrScale = 0f
                    }); // PowerBarBackground
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(192f, 46f + (270f - (270f * (float)powerStoredRatio)) * 0.5f) * scale + centerPos,
                        Size = new Vector2(30f, 270f * (float)powerStoredRatio) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }); // PowerBarForeground
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(126f, 46f) * scale + centerPos,
                        Size = new Vector2(30f, 270f) * scale,
                        Color = new Color(192, 192, 192, 255),
                        RotationOrScale = 0f
                    }); // HydrogenBarBackground
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        // Position needs to be adjusted based on the ratio of fuel filled
                        Position = new Vector2(126f, 46f + (270f - (270f * (float)fuelFilledRatio)) * 0.5f) * scale + centerPos,
                        Size = new Vector2(30f, 270f * (float)fuelFilledRatio) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }); // HydrogenBarForeground
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "IconEnergy",
                        Position = new Vector2(192f, 212f) * scale + centerPos,
                        Size = new Vector2(35f, 35f) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }); // sprite5Copy
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "IconHydrogen",
                        Position = new Vector2(126f, 212f) * scale + centerPos,
                        Size = new Vector2(35f, 35f) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }); // sprite5
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = velocityFuelPowerString,
                        Position = new Vector2(242f, -78f) * scale + centerPos,
                        Color = new Color(0, 255, 0, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // Telemetry
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = "Status: ",
                        Position = new Vector2(64f, -220f) * scale + centerPos,
                        Color = new Color(0, 255, 0, 255),
                        FontId = "Debug",
                        RotationOrScale = 1.56f * scale
                    }); // text3
                }
            }
        }
    }
}
