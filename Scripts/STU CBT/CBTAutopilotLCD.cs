using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBTAutopilotLCD : STUDisplay
        {
            public static Action<string> echo;

            public CBTAutopilotLCD(Action<string> Echo, IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize)
            {
                echo = Echo;
            }

            public void SetupDrawSurface(IMyTextSurface surface, bool enabled)
            {
                if (enabled) {
                    // Draw background color
                    //  surface.ScriptBackgroundColor = new Color(0, 121, 0, 255);
                    // Set content type
                    surface.ContentType = ContentType.SCRIPT;
                    // Set script to none
                    surface.Script = "";
                }
                else {
                    // Draw background color
                    //  surface.ScriptBackgroundColor = new Color(106, 0, 0, 255);
                    // Set content type
                    surface.ContentType = ContentType.SCRIPT;
                    // Set script to none
                    surface.Script = "";
                }
            }

            public void DrawAutopilotEnabledSprite(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                MySprite background_sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(0f, 0f),
                    Size = new Vector2(ScreenWidth, ScreenHeight),
                    Color = new Color(0, 128, 0, 255),
                    RotationOrScale = 0f
                };
                MySprite circle = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(180f, 180f) * scale,
                    Color = new Color(0, 255, 0, 255),
                    RotationOrScale = 0f
                }; 
                MySprite letter_A = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = "A",
                    Position = new Vector2(-54f, -102f) * scale + centerPos,
                    Color = new Color(0, 255, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = 6f * scale
                };

                AlignCenterWithinParent(background_sprite, ref circle);
                AlignCenterWithinParent(background_sprite, ref letter_A);

                frame.Add(background_sprite);
                frame.Add(circle);
                frame.Add(letter_A);
            }


            public void DrawAutopilotDisabledSprite(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                MySprite background_sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(0f + ScreenWidth / 2, 0f + ScreenHeight / 2 + 20),
                    Size = new Vector2(ScreenWidth, ScreenHeight) * scale,
                    Color = new Color(106, 0, 0, 255),
                    RotationOrScale = 0f
                };
                MySprite circle = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(180f, 180f) * scale,
                    Color = new Color(255, 0, 0, 255),
                    RotationOrScale = 0f
                }; // circle
                MySprite letter_M = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = "M",
                    Position = new Vector2(-57f, -84f) * scale + centerPos,
                    Color = new Color(255, 0, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = 5f * scale
                }; // textM

                AlignCenterWithinParent(background_sprite, ref circle);
                AlignCenterWithinParent(background_sprite, ref letter_M);

                frame.Add(background_sprite);
                frame.Add(circle);
                frame.Add(letter_M);
            }

        }
    }
    
}
