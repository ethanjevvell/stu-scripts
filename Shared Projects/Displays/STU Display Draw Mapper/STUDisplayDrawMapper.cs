using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public class STUDisplayDrawMapper {

            private static string errorMessage = "";

            public static Action<MySpriteDrawFrame, Vector2, float> DefaultErrorScreen = (frame, centerPos, scale) => {
                frame.Add(new MySprite() {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(0, 0),
                    Size = new Vector2(5000, 5000),
                    Color = Color.Red,
                    Alignment = TextAlignment.CENTER,
                });
                frame.Add(new MySprite() {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = errorMessage,
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Debug",
                    RotationOrScale = 1f * scale
                }); // text1
            };

            public Dictionary<string, Action<MySpriteDrawFrame, Vector2, float>> DisplayDrawMapper = new Dictionary<string, Action<MySpriteDrawFrame, Vector2, float>>();

            public STUDisplayDrawMapper() { }
            public STUDisplayDrawMapper(Dictionary<string, Action<MySpriteDrawFrame, Vector2, float>> drawMapper) {
                DisplayDrawMapper = drawMapper;
            }

            public void Add(string displayType, Action<MySpriteDrawFrame, Vector2, float> drawFunction) {
                // try to add the key value pari, but if the key already exists, just do nothing
                try {
                    DisplayDrawMapper.Add(displayType, drawFunction);
                } catch { }
            }

            public Action<MySpriteDrawFrame, Vector2, float> GetDrawFunction(IMyTerminalBlock block, int displayIndex) {

                var displayIdentifier = STUDisplayType.GetDisplayIdentifier(block, displayIndex);
                Action<MySpriteDrawFrame, Vector2, float> drawFunction;

                try {
                    drawFunction = DisplayDrawMapper[displayIdentifier];
                } catch {
                    errorMessage = $"INVALID DISPLAY: {block.DefinitionDisplayNameText}\n";
                    errorMessage += $"Available display types: {string.Join(", ", DisplayDrawMapper.Keys)} \n";
                    return DefaultErrorScreen;
                }
                return drawFunction;

            }

        }
    }
}
