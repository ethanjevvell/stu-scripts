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
        public partial class CBTManeuverQueueLCD : STUDisplay
        {
            public static Action<string> echo;

            public CBTManeuverQueueLCD(Action<string> Echo, IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize)
            {
                echo = Echo;
            }

            public ManeuverQueueData ManeuverQueueData;

            private MySprite CurrentManeuverName;
            private MySprite CurrentManeuverInitStatus;
            private MySprite CurrentManeuverRunStatus;
            private MySprite CurrentManeuverCloseoutStatus;
            private MySprite FirstManeuverName;
            private MySprite SecondManeuverName;
            private MySprite ThirdManeuverName;
            private MySprite FourthManeuverName;
            private MySprite Continuation;

            #region Build Sprites
            public void BuildCurrentManeuverName(string currentManeuverName)
            {
                CurrentManeuverName = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = currentManeuverName,
                    Position = new Vector2(0, -218f), //  irrelevant due to AlignTopWithinParent
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 0.75f
                };
            }
            public void BuildCurrentManeuverInitStatus(bool currentManeuverInitStatus)
            {
                Color color;
                if (currentManeuverInitStatus)
                    color = new Color(255, 255, 255, 255);
                else 
                    color = new Color(64, 64, 64, 255);
                CurrentManeuverInitStatus = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(48f, 74f),
                    Size = new Vector2(64f, 7f),
                    Color = color,
                    RotationOrScale = 0f
                };
            }
            public void BuildCurrentManeuverRunStatus(bool currentManeuverRunStatus)
            {
                Color color;
                if (currentManeuverRunStatus)
                    color = new Color(255, 255, 255, 255);
                else 
                    color = new Color(64, 64, 64, 255);
                CurrentManeuverRunStatus = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(128f, 74f),
                    Size = new Vector2(64f, 7f),
                    Color = color,
                    RotationOrScale = 0f
                };
            }
            public void BuildCurrentManeuverCloseoutStatus(bool currentManeuverCloseoutStatus)
            {
                Color color;
                if (currentManeuverCloseoutStatus)
                    color = new Color(255, 255, 255, 255);
                else 
                    color = new Color(64, 64, 64, 255);
                CurrentManeuverCloseoutStatus = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = new Vector2(208f, 74f),
                    Size = new Vector2(64f, 7f),
                    Color = color,
                    RotationOrScale = 0f
                };
            }
            public void BuildFirstManeuverName(string firstManeuverName)
            {
                FirstManeuverName = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = firstManeuverName,
                    Position = new Vector2(16f, 90f),
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 0.6f
                };
            }
            public void BuildSecondManeuverName(string secondManeuverName)
            {
                SecondManeuverName = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = secondManeuverName,
                    Position = new Vector2(16f, 110f),
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 0.6f
                };
            }
            public void BuildThirdManeuverName(string thirdManeuverName)
            {
                ThirdManeuverName = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = thirdManeuverName,
                    Position = new Vector2(16f, 130f),
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 0.6f
                };
            }
            public void BuildFourthManeuverName(string fourthManeuverName)
            {
                FourthManeuverName = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = fourthManeuverName,
                    Position = new Vector2(16f, 150f),
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 0.6f
                };
            }
            // if continuation is true, make the sprite say "..."
            // if false, make the sprite say "End of Queue"
            public void BuildContinuation(string str)
            {
                Continuation = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = str,
                    Position = new Vector2(16f, 170f), 
                    Color = new Color(64, 64, 64, 255),
                    FontId = "Monospace",
                    RotationOrScale = 0.5f
                };
            }
            #endregion

            public void LoadManeuverQueueData(ManeuverQueueData data)
            {
                BuildCurrentManeuverName(data.CurrentManeuverName);
                BuildCurrentManeuverInitStatus(data.CurrentManeuverInitStatus);
                BuildCurrentManeuverRunStatus(data.CurrentManeuverRunStatus);
                BuildCurrentManeuverCloseoutStatus(data.CurrentManeuverCloseoutStatus);

                // determine what to pass into the first maneuver name
                if(data.FirstManeuverName != null) { BuildFirstManeuverName($"1. {data.FirstManeuverName}"); }
                else if (data.FirstManeuverName == null && data.CurrentManeuverName != null) { BuildFirstManeuverName("-- END OF QUEUE --"); }
                else { BuildFirstManeuverName(data.FirstManeuverName); }

                // determine what to pass into the second maneuver name
                if (data.SecondManeuverName != null) { BuildSecondManeuverName($"2. {data.SecondManeuverName}"); }
                else if (data.SecondManeuverName == null && data.FirstManeuverName != null) { BuildSecondManeuverName("-- END OF QUEUE --"); }
                else { BuildSecondManeuverName(data.SecondManeuverName); }

                // determine what to pass into the third maneuver name
                if (data.ThirdManeuverName != null) { BuildThirdManeuverName($"3. {data.ThirdManeuverName}"); }
                else if (data.ThirdManeuverName == null && data.SecondManeuverName != null) { BuildThirdManeuverName("-- END OF QUEUE --"); }
                else { BuildThirdManeuverName(data.ThirdManeuverName); }

                // determine what to pass into the fourth maneuver name
                if (data.FourthManeuverName != null) { BuildFourthManeuverName($"4. {data.FourthManeuverName}"); }
                else if (data.FourthManeuverName == null && data.ThirdManeuverName != null) { BuildFourthManeuverName("-- END OF QUEUE --"); }
                else { BuildFourthManeuverName(data.FourthManeuverName); }

                // determine what to pass in for the continuation field
                if (data.Continuation == true) { BuildContinuation("(CONTINUED)"); }
                else if (data.Continuation == false && data.FourthManeuverName != null) { BuildContinuation("-- END OF QUEUE --"); }
                else { BuildContinuation(""); }
            }

            public void BuildManeuverQueueScreen(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                MySprite background_sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = centerPos,
                    Size = new Vector2(ScreenWidth, ScreenHeight),
                    Color = new Color(0, 0, 0, 255),
                    RotationOrScale = 0f
                };

                AlignTopWithinParent(background_sprite, ref CurrentManeuverName);

                frame.Add(background_sprite);
                frame.Add(CurrentManeuverName);
                frame.Add(CurrentManeuverInitStatus);
                frame.Add(CurrentManeuverRunStatus);
                frame.Add(CurrentManeuverCloseoutStatus);
                frame.Add(FirstManeuverName);
                frame.Add(SecondManeuverName);
                frame.Add(ThirdManeuverName);
                frame.Add(FourthManeuverName);
                frame.Add(Continuation);
            }
        }
    }
}
