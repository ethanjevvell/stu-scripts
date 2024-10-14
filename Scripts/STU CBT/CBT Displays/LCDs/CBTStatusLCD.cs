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
        public partial class CBTStatusLCD : STUDisplay
        {
            public static Action<string> echo;

            public CBTStatusLCD(Action<string> Echo, IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize)
            {
                echo = Echo;
            }

            public void BuildScreen(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                MySprite background = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = TopLeft + new Vector2(0, ScreenHeight / 2),
                    Size = new Vector2(ScreenWidth, ScreenHeight),
                    Color = new Color(0, 0, 0)
                };
                MySprite horizontal_line = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = TopLeft + new Vector2(0, ScreenHeight / 2 + 1),
                    Size = new Vector2(ScreenWidth, 2),
                    Color = new Color(64, 64, 64)
                };
                

                frame.Add(background);
                frame.Add(horizontal_line);
                

            }

        }
    }
}
