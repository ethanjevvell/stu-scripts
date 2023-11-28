using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {


        public enum DisplayType {
            LCD_PANEL,
            WIDE_LCD,
        }

        public class STUSpriteDisplayAdapter {

            public static Dictionary<string, DisplayType> DisplayTypeMap = new Dictionary<string, DisplayType>() {
                { "ScreenArea", DisplayType.LCD_PANEL }
            };

            public delegate void DrawerFunction(MySpriteDrawFrame frame, Vector2 centerPos, float scale);

            public float Scale { get; set; }
            public Vector2 CenterPos { get; set; }
            public DrawerFunction Drawer { get; set; }

            public STUSpriteDisplayAdapter() { }

            public STUSpriteDisplayAdapter(DrawerFunction drawer) {
                Drawer = drawer;
            }

            public void Draw(MySpriteDrawFrame frame) {
                Drawer?.Invoke(frame, CenterPos, Scale);
            }

        }
    }
}

