using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {


        public Program() {
        }

        public void Main() {
            Echo($"Last runtime: {Runtime.LastRunTimeMs} ms");

            var oneByOneDisplay = GridTerminalSystem.GetBlockWithName("ONE_BY_ONE_LCD") as IMyTextSurfaceProvider;
            var oneByOneDisplaySurface = oneByOneDisplay.GetSurface(0);

            var wideDisplay = GridTerminalSystem.GetBlockWithName("WIDE_LCD") as IMyTextSurfaceProvider;
            var wideDisplaySurface = wideDisplay.GetSurface(0);

            var oneByOneDisplayTest = new TestDisplay(oneByOneDisplaySurface, Echo);
            oneByOneDisplayTest.Test();

            var wideDisplayTest = new TestDisplay(wideDisplaySurface, Echo);
            wideDisplayTest.Test();

        }

        public partial class TestDisplay : STUDisplay {

            public static float Velocity = 0;
            public static Dictionary<DisplayType, Action<MySpriteDrawFrame, Vector2, float>> DisplayRouter = new Dictionary<DisplayType, Action<MySpriteDrawFrame, Vector2, float>>() {
                { DisplayType.LCD_PANEL, DrawStandardLCDSprites },
                { DisplayType.WIDE_LCD, DrawWideLCDSprites }
            };

            Action<MySpriteDrawFrame, Vector2, float> Drawer;
            Action<string> Echo;

            public TestDisplay(IMyTextSurface surface, Action<string> echo, string font = "Monospace", float fontSize = 1f) : base(surface, font, fontSize) {

                Echo = echo;
                Surface.BackgroundColor = Color.Black;
                Surface.Script = "";
                Drawer = DisplayRouter[STUSpriteDisplayAdapter.DisplayTypeMap[Surface.DisplayName]];

            }

            public void Test() {
                Echo("Start test");
                StartFrame();
                Drawer.Invoke(CurrentFrame, Viewport.Center, 1f);
                EndAndPaintFrame();

            }
        }

    }

}

