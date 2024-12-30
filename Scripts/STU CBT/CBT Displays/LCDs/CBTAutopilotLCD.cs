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

            public void DrawAutopilotEnabledSprite(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                MySprite background_sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = centerPos,
                    Size = new Vector2(ScreenWidth, ScreenHeight),
                    Color = new Color(0, 128, 0, 255),
                    RotationOrScale = 0f
                };
                MySprite circle = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos, // this line is irrelevant because of AlignCenterWithinParent
                    Size = new Vector2(180f, 180f) * scale,
                    Color = new Color(0, 255, 0, 255),
                    RotationOrScale = 0f
                }; 
                MySprite letter_A = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = CBT.GetAutopilotState().ToString(),
                    Position = new Vector2(-54f, -102f) * scale + centerPos, // this line is irrelevant because of AlignCenterWithinParent
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
                    Position = centerPos,
                    Size = new Vector2(ScreenWidth, ScreenHeight) * scale,
                    Color = new Color(106, 0, 0, 255),
                    RotationOrScale = 0f
                };
                MySprite circle = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "CircleHollow",
                    Position = new Vector2(0f, 0f) * scale + centerPos, // this line is irrelevant because of AlignCenterWithinParent
                    Size = new Vector2(180f, 180f) * scale,
                    Color = new Color(255, 0, 0, 255),
                    RotationOrScale = 0f
                }; // circle
                MySprite ap_state = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = "0",
                    Position = new Vector2(-57f, -84f) * scale + centerPos, // this line is irrelevant because of AlignCenterWithinParent
                    Color = new Color(255, 0, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = 5f * scale
                }; // textM

                AlignCenterWithinParent(background_sprite, ref circle);
                AlignCenterWithinParent(background_sprite, ref ap_state);

                frame.Add(background_sprite);
                frame.Add(circle);
                frame.Add(ap_state);
            }

            public void DrawAutopilotStatus(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                string thrustersState;
                bool thrustersOn = CBT.FlightController.HasThrusterControl;
                Color thrustersBGColor;
                string gyroState;
                bool gyroOn = CBT.FlightController.HasGyroControl;
                Color gyroBGColor;
                string dampenersState;
                bool dampenersOn = CBT.FlightController.RemoteControl.DampenersOverride;
                Color dampenersBGColor;

                if (thrustersOn)
                {
                    thrustersState = "Thrusters ON";
                    thrustersBGColor = new Color(0, 255, 0, 255);
                }
                else
                {
                    thrustersState = "Thrusters OFF";
                    thrustersBGColor = new Color(255, 0, 0, 255);
                }

                if (gyroOn)
                {
                    gyroState = "Gyros ON";
                    gyroBGColor = new Color(0, 255, 0, 255);
                }
                else
                {
                    gyroState = "Gyros OFF";
                    gyroBGColor = new Color(255, 0, 0, 255);
                }

                if (dampenersOn)
                {
                    dampenersState = "Dampeners ON";
                    dampenersBGColor = new Color(0, 255, 0, 255);
                }
                else
                {
                    dampenersState = "Dampeners OFF";
                    dampenersBGColor = new Color(255, 0, 0, 255);
                }
                
                MySprite background_sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = centerPos,
                    Size = new Vector2(ScreenWidth, ScreenHeight) * scale,
                    Color = new Color(0, 0, 0, 255),
                    RotationOrScale = 0f
                };
                MySprite title = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = "AUTOPILOT",
                    Position = new Vector2(0f, -105f) * scale + centerPos, 
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Debug",
                    RotationOrScale = scale
                };
                MySprite thrusters = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = thrustersState,
                    Position = new Vector2(0f, -51f) * scale + centerPos,
                    Color = new Color(0, 0, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = scale
                };
                MySprite thrustersBG = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(0f, -36f) * scale + centerPos,
                    Size = new Vector2(ScreenWidth, 31f) * scale,
                    Color = thrustersBGColor,
                    RotationOrScale = 0f
                }; // Thrusters BG
                MySprite gyro = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = gyroState,
                    Position = new Vector2(0f, -18f) * scale + centerPos,
                    Color = new Color(0, 0, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = scale
                };
                MySprite gyroBG = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(0f, 0f) * scale + centerPos,
                    Size = new Vector2(ScreenWidth, 31f) * scale,
                    Color = gyroBGColor,
                    RotationOrScale = 0f
                }; // Gyro BG
                MySprite dampeners = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = dampenersState,
                    Position = new Vector2(0f, 18f) * scale + centerPos,
                    Color = new Color(0, 0, 0, 255),
                    FontId = "Debug",
                    RotationOrScale = scale
                };
                MySprite dampenersBG = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(0f, 36f) * scale + centerPos,
                    Size = new Vector2(ScreenWidth, 31f) * scale,
                    Color = dampenersBGColor,
                    RotationOrScale = 0f
                }; // Dampeners BG
                MySprite CBTStateMachineStatus = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = CBT.CurrentPhase.ToString(),
                    Position = new Vector2(0f, 51f) * scale + centerPos,
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Debug",
                    RotationOrScale = scale
                };

                frame.Add(background_sprite);
                frame.Add(title);
                frame.Add(thrustersBG);
                frame.Add(thrusters);
                frame.Add(gyroBG);
                frame.Add(gyro);
                frame.Add(dampenersBG);
                frame.Add(dampeners);
                frame.Add(CBTStateMachineStatus);
            }
        }
    }
}
