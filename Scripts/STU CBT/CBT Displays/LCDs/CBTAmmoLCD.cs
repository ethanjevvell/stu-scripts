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
        public partial class CBTAmmoLCD : STUDisplay
        {
            public static Action<string> echo;

            public CBTAmmoLCD(Action<string> Echo, IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize)
            {
                echo = Echo;
            }

            int GatlingAmmo;
            int AssaultAmmo;
            int RailgunAmmo;

            public void LoadAmmoData(int gatlingAmmo, int assaultAmmo, int railgunAmmo)
            {
                GatlingAmmo = gatlingAmmo;
                AssaultAmmo = assaultAmmo;
                RailgunAmmo = railgunAmmo;
            }

            public void BuildScreen(MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
            {
                MySprite background = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = TopLeft + new Vector2(0,ScreenHeight / 2),
                    Size = new Vector2(ScreenWidth, ScreenHeight),
                    Color = new Color(0,0,0)
                };
                MySprite horizontal_line = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = TopLeft + new Vector2(0, ScreenHeight / 2 + 1),
                    Size = new Vector2(ScreenWidth, 2),
                    Color = new Color(64, 64, 64)
                };
                MySprite vertical_line_1 = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = TopLeft + new Vector2(ScreenWidth / 3 - 1, 0),
                    Size = new Vector2(2, ScreenHeight),
                    Color = new Color(64, 64, 64)
                };
                MySprite vertical_line_2 = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = TopLeft + new Vector2(ScreenWidth / 3 * 2 - 1, 0),
                    Size = new Vector2(2, ScreenHeight),
                    Color = new Color(64, 64, 64)
                };
                MySprite GAT_text = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "GAT",
                    Position = TopLeft + new Vector2(ScreenWidth / 6 - GetTextSpriteWidth("GAT") / 2, 1),
                    Color = new Color(255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 1
                };
                MySprite GAT_ammo_amount = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = GatlingAmmo.ToString(),
                    Position = TopLeft + new Vector2(ScreenWidth / 6 - GetTextSpriteWidth(GatlingAmmo.ToString()) / 2, ScreenHeight / 4 + 1),
                    Color = new Color(255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 1
                };
                MySprite AT_text = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "AT",
                    Position = TopLeft + new Vector2(ScreenWidth / 2 - GetTextSpriteWidth("AT") / 2, 1),
                    Color = new Color(255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 1
                };
                MySprite AT_ammo_amount = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = AssaultAmmo.ToString(),
                    Position = TopLeft + new Vector2(ScreenWidth / 2 - GetTextSpriteWidth(AssaultAmmo.ToString()) / 2, ScreenHeight / 4 + 1),
                    Color = new Color(255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 1
                };
                MySprite RG_text = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "RG",
                    Position = TopLeft + new Vector2(ScreenWidth / 6 * 5 - GetTextSpriteWidth("RG") / 2, 1),
                    Color = new Color(255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 1
                };
                MySprite RG_ammo_amount = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = RailgunAmmo.ToString(),
                    Position = TopLeft + new Vector2(ScreenWidth / 6 * 5 - GetTextSpriteWidth(RailgunAmmo.ToString()) / 2, ScreenHeight / 4 + 1),
                    Color = new Color(255, 255, 255),
                    FontId = "Monospace",
                    RotationOrScale = 1
                };


                frame.Add(background);
                frame.Add(horizontal_line);
                frame.Add(vertical_line_1);
                frame.Add(vertical_line_2);
                frame.Add(GAT_text);
                frame.Add(GAT_ammo_amount);
                frame.Add(AT_text);
                frame.Add(AT_ammo_amount);
                frame.Add(RG_text);
                frame.Add(RG_ammo_amount);

            }
        }
    }
}
