﻿using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class MainLCD {
            public static class LargeWideLCD {
                public static void ScreenArea(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f) {

                    double fuelFilledRatio = CurrentFuel / FuelCapacity;
                    double powerStoredRatio = CurrentPower / PowerCapacity;
                    string velocityFuelPowerString = $"Velocity:\n   V_x = {VelocityComponents.X.ToString("F2")}\n   V_y = {VelocityComponents.Y.ToString("F2")}\n   V_z = {VelocityComponents.Z.ToString("F2")}\nFuel: {(int)(fuelFilledRatio * 100)}%\nPower: {(int)(100 * (CurrentPower / PowerCapacity))}%";
                    string accelerationString = $"Acceleration:\n   A_x = {AccelerationComponents.X.ToString("F2")}\n   A_y = {AccelerationComponents.Y.ToString("F2")}\n   A_z = {AccelerationComponents.Z.ToString("F2")}\n";

                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = "LIGMA MK-I",
                        Position = new Vector2(-472f, -234f) * scale + centerPos,
                        Color = new Color(0, 255, 0, 255),
                        FontId = "Debug",
                        RotationOrScale = 2f * scale
                    }); // text1
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(-400f, 40f) * scale + centerPos,
                        Size = new Vector2(30f, 270f) * scale,
                        Color = new Color(192, 192, 192, 255),
                        RotationOrScale = 0f
                    }); // PowerBarBackground
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(-400f, 40f + (270f - (270f * (float)powerStoredRatio)) * 0.5f) * scale + centerPos,
                        Size = new Vector2(30f, 270f * (float)powerStoredRatio) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }); // PowerBarForeground
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(-450f, 40f) * scale + centerPos,
                        Size = new Vector2(30f, 270f) * scale,
                        Color = new Color(192, 192, 192, 255),
                        RotationOrScale = 0f
                    }); // HydrogenBarBackground
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "SquareSimple",
                        Position = new Vector2(-450f, 40f + (270f - (270f * (float)fuelFilledRatio)) * 0.5f) * scale + centerPos,
                        Size = new Vector2(30f, 270f * (float)fuelFilledRatio) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }); // HydrogenBarForeground
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "IconEnergy",
                        Position = new Vector2(-400f, 210f) * scale + centerPos,
                        Size = new Vector2(35f, 35f) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }); // Electricity
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXTURE,
                        Alignment = TextAlignment.CENTER,
                        Data = "IconHydrogen",
                        Position = new Vector2(-450f, 210f) * scale + centerPos,
                        Size = new Vector2(35f, 35f) * scale,
                        Color = new Color(0, 255, 0, 255),
                        RotationOrScale = 0f
                    }); // H2
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = velocityFuelPowerString,
                        Position = new Vector2(-350f, -96f) * scale + centerPos,
                        Color = new Color(0, 255, 0, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // Telemetry
                    frame.Add(new MySprite() {
                        Type = SpriteType.TEXT,
                        Alignment = TextAlignment.LEFT,
                        Data = accelerationString,
                        Position = new Vector2(-150f, -96f) * scale + centerPos,
                        Color = new Color(0, 255, 0, 255),
                        FontId = "Debug",
                        RotationOrScale = 1f * scale
                    }); // Telemetry

                }
            }
        }
    }
}
