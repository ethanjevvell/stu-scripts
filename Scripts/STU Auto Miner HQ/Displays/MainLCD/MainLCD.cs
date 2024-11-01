using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class MainLCD : STUDisplay {


            public MainLCD(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {
            }

            public void Update(Dictionary<string, MiningDroneData> droneData) {
                StartFrame();
                DrawMainBackground(Center);
                DrawDronePlaquards(droneData);
                EndAndPaintFrame();
            }

            void DrawDronePlaquards(Dictionary<string, MiningDroneData> droneData) {
                int index = 0;
                foreach (var drone in droneData) {
                    DrawPlaquered(index, drone.Value, Center);
                    index++;
                }
            }

            void DrawMainBackground(Vector2 centerPos, float scale = 1f) {
                CurrentFrame.Add(new MySprite(SpriteType.TEXT, "S.T.U. Auto Miner Suite", new Vector2(-144f, -230f) * scale + centerPos, null, new Color(255, 255, 255, 255), "Debug", TextAlignment.LEFT, 1f * scale)); // MainPageTitle
                CurrentFrame.Add(new MySprite(SpriteType.TEXT, "---------------------------------------", new Vector2(-156f, -200f) * scale + centerPos, null, new Color(255, 255, 255, 255), "Debug", TextAlignment.LEFT, 1f * scale)); // DashedLine
            }

            void DrawPlaquered(int index, MiningDroneData droneData, Vector2 centerPos, float scale = 1f) {
                float plaqueredHeight = 92f;
                float verticalOffset = index * (plaqueredHeight + 10f);
                double powerPercentage = droneData.PowerMegawatts / droneData.PowerCapacity * 100;
                double hydrogenPercentage = droneData.HydrogenLiters / droneData.HydrogenCapacity * 100;
                CurrentFrame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(30f, -114f + verticalOffset) * scale + centerPos, new Vector2(420f, plaqueredHeight) * scale, new Color(192, 192, 192, 255), null, TextAlignment.CENTER, 0f)); // Plaguered
                CurrentFrame.Add(new MySprite(SpriteType.TEXT, $"{index + 1}", new Vector2(-224f, -132f + verticalOffset) * scale + centerPos, null, new Color(0, 128, 255, 255), "Debug", TextAlignment.LEFT, 1f * scale)); // DroneNumber
                CurrentFrame.Add(new MySprite(SpriteType.TEXT, droneData.Name, new Vector2(-156f, -132f + verticalOffset) * scale + centerPos, null, new Color(0, 128, 255, 255), "Debug", TextAlignment.LEFT, 1f * scale)); // DroneName
                CurrentFrame.Add(new MySprite(SpriteType.TEXTURE, "IconEnergy", new Vector2(150f, -98f + verticalOffset) * scale + centerPos, new Vector2(25f, 25f) * scale, new Color(255, 255, 0, 255), null, TextAlignment.CENTER, 0f)); // power
                CurrentFrame.Add(new MySprite(SpriteType.TEXTURE, "IconHydrogen", new Vector2(154f, -134f + verticalOffset) * scale + centerPos, new Vector2(25f, 25f) * scale, new Color(0, 128, 255, 255), null, TextAlignment.CENTER, 0f)); // h2
                CurrentFrame.Add(new MySprite(SpriteType.TEXT, $"{Math.Round(powerPercentage)}", new Vector2(186f, -114f + verticalOffset) * scale + centerPos, null, new Color(255, 255, 255, 255), "Debug", TextAlignment.LEFT, 1f * scale)); // powerPercentage
                CurrentFrame.Add(new MySprite(SpriteType.TEXT, $"{Math.Round(hydrogenPercentage)}", new Vector2(186f, -148f + verticalOffset) * scale + centerPos, null, new Color(255, 255, 255, 255), "Debug", TextAlignment.LEFT, 1f * scale)); // h2Percentage
                CurrentFrame.Add(new MySprite(SpriteType.TEXT, droneData.State, new Vector2(-50f, -132f + verticalOffset) * scale + centerPos, null, new Color(255, 255, 255, 255), "Debug", TextAlignment.LEFT, 1f * scale)); // State
            }

        }
    }
}
