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

            private MySprite CurrentManeuverName = new MySprite();
            private MySprite CurrentManeuverInitStatus = new MySprite();
            private MySprite CurrentManeuverRunStatus = new MySprite();
            private MySprite CurrentManeuverCloseoutStatus = new MySprite();
            private MySprite FirstManeuverName = new MySprite();
            private MySprite SecondManeuverName = new MySprite();
            private MySprite ThirdManeuverName = new MySprite();
            private MySprite FourthManeuverName = new MySprite();
            private MySprite Continuation = new MySprite();
            private MySprite EndOfQueue = new MySprite();

            public void BuildCurrentManeuverName(string currentManeuverName)
            {
                if (currentManeuverName == null)
                {
                    currentManeuverName = "Queue empty.";
                }
                CurrentManeuverName = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.LEFT,
                    Data = currentManeuverName,
                    Position = new Vector2(0f, -100f),
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Debug",
                    RotationOrScale = 1f
                };
            }

            public void LoadManeuverQueueData(
                string currentManeuverName = null,
                bool currentManeuverInitStatus = false,
                bool currentManeuverRunStatus = false,
                bool currentManeuverCloseoutStatus = false,
                string firstManeuverName = null,
                string secondManeuverName = null,
                string thirdManeuverName = null,
                string fourthManeuverName = null,
                bool hasMoreInTheQueue = false)
            {
                if (currentManeuverName != null) { BuildCurrentManeuverName(currentManeuverName); }
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
                MySprite title = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Alignment = TextAlignment.CENTER,
                    Data = "Maneuver Queue",
                    Position = new Vector2(0f, -100f) * scale + centerPos, // this line is irrelevant because of AlignCenterWithinParent
                    Color = new Color(255, 255, 255, 255),
                    FontId = "Debug",
                    RotationOrScale = 2f * scale
                };

                AlignCenterWithinParent(background_sprite, ref title);

                frame.Add(background_sprite);
                frame.Add(title);
            }

        }
    }

}
