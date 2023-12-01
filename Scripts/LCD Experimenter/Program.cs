
using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyTerminalBlock pb;
        IMyTerminalBlock lcd;
        IMyTerminalBlock wide_lcd;

        MainDisplay me;
        MainDisplay oneByOneDisplay;
        MainDisplay wideDisplay;

        public Program() {
            pb = Me;
            lcd = GridTerminalSystem.GetBlockWithName("ONE_BY_ONE_LCD");
            wide_lcd = GridTerminalSystem.GetBlockWithName("WIDE_LCD");

            me = new MainDisplay(pb, 1, Echo);
            oneByOneDisplay = new MainDisplay(lcd, 0, Echo);
            wideDisplay = new MainDisplay(wide_lcd, 0, Echo);
        }

        public void Main() {
            me.Test();
            oneByOneDisplay.Test();
            wideDisplay.Test();
            MainDisplay.UpdateVelocity();
        }

        public partial class MainDisplay : STUDisplay {

            public static float Velocity = 420;

            public static STUDisplayDrawMapper TestDisplayDrawMapper = new STUDisplayDrawMapper {
                DisplayDrawMapper = {
                    { STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.LargeLCDPanel, STUSubDisplay.ScreenArea), LargeStandardLCDSprites.ScreenArea },
                    { STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.LargeLCDPanelWide, STUSubDisplay.ScreenArea), LargeWideLCDSprites.ScreenArea },
                    { STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.LargeProgrammableBlock, STUSubDisplay.LargeDisplay), LargeProgrammableBlockSprites.LargeDisplay }
                }
            };

            public Action<MySpriteDrawFrame, Vector2, float> Drawer;
            public Action<string> Echo;

            public MainDisplay(IMyTerminalBlock block, int displayIndex, Action<string> echo, string font = "Monospace", float fontSize = 1f) : base(block, displayIndex, font, fontSize) {
                Echo = echo;
                Drawer = TestDisplayDrawMapper.GetDrawFunction(block, displayIndex);
            }

            public static void UpdateVelocity() {
                Velocity += 1;
            }

            public void Test() {
                StartFrame();
                Drawer.Invoke(CurrentFrame, Viewport.Center, 1f);
                EndAndPaintFrame();
            }
        }

    }

}
