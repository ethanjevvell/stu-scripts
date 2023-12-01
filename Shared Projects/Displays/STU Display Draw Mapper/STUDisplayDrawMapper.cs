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
                    Size = new Vector2(5000, 5000),
                    Color = Color.Red,
                    Alignment = TextAlignment.CENTER,
                });
                frame.Add(new MySprite() {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = errorMessage,
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

            /// <summary>
            /// Adds a display type to the mapper; be sure to use the STUDisplayType.CreateDisplayIdentifier method to create the display identifier for your screen.
            /// If possible, don't use this function. Instead, instantiate the STUDisplayDrawMapper with its mappings directly defined in the instantiation. 
            /// </summary>
            /// <param name="displayType"></param>
            /// <param name="drawFunction"></param>
            public void Add(string displayType, Action<MySpriteDrawFrame, Vector2, float> drawFunction) {
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
                    return DefaultErrorScreen;
                }
                return drawFunction;

            }

        }
    }
}
