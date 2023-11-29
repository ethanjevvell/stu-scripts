using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {


        public Program() {
        }

        public void Main() {
            Echo($"Last runtime: {Runtime.LastRunTimeMs} ms");

            var pbScreen = Me.GetSurface(0);
            var pbScreenTest = new TestDisplay(pbScreen, Echo);
            Echo(pbScreenTest.Surface.DisplayName);
            var tempBlock = Me as IMyTerminalBlock;
            Echo($"Display name: {tempBlock.DisplayName}");
            Echo($"Definition display name text: {tempBlock.DefinitionDisplayNameText}");
            Echo($"Display name text: {tempBlock.DisplayNameText}");
            Echo($"Name: {tempBlock.Name}");
            Echo($"Grid size: {tempBlock.CubeGrid.GridSizeEnum}");
            Echo($"Block subtype name: {tempBlock.BlockDefinition.SubtypeName}");
            Echo($"Block subtype id: {tempBlock.BlockDefinition.SubtypeId}");
            Echo(tempBlock.CubeGrid.GridSizeEnum == MyCubeSize.Large ? "Large" : "Small");
            // pbScreenTest.Test();

            Echo("Start one by one display");

            var oneByOneDisplay = GridTerminalSystem.GetBlockWithName("ONE_BY_ONE_LCD") as IMyTextPanel;
            var tempBlock2 = oneByOneDisplay as IMyTerminalBlock;
            Echo($"Display name: {tempBlock2.DisplayName}");
            Echo($"Definition display name text: {tempBlock2.DefinitionDisplayNameText}");
            Echo($"Display name text: {tempBlock2.DisplayNameText}");
            Echo($"Name: {tempBlock2.Name}");
            Echo($"Grid size: {tempBlock2.CubeGrid.GridSizeEnum}");
            Echo($"Block subtype name: {tempBlock2.BlockDefinition.SubtypeName}");
            Echo($"Block subtype id: {tempBlock2.BlockDefinition.SubtypeId}");
            Echo(tempBlock2.CubeGrid.GridSizeEnum == MyCubeSize.Large ? "Large" : "Small");

            var wideDisplay = GridTerminalSystem.GetBlockWithName("WIDE_LCD") as IMyTextPanel;

            if (oneByOneDisplay != null) {
                var oneByOneDisplayTest = new TestDisplay(oneByOneDisplay, Echo);
                Echo(oneByOneDisplay.BlockDefinition.SubtypeId);
                oneByOneDisplayTest.Test();
            }

            if (wideDisplay != null) {
                var wideDisplayTest = new TestDisplay(wideDisplay, Echo);
                Echo(wideDisplay.BlockDefinition.SubtypeId);
                wideDisplayTest.Test();
            }

        }

        // LATER TONIGHT!
        // LCD blocks are not handled the same way as terminal blocks with displays on them
        // An LCD block is of the type IMyTextPanel, and you can figure out which type of LCD
        // you're working with by .BlockDefinition.SubtypeId
        // On the other hand, displays on terminal blocks (such as PBs, cockpits, etc.) need to be
        // cast as IMyTextSurface, and you can figure out which type of LCD you're working with by
        // using the property .DisplayName
        // AFTER GYM, we need to find a way (likely with method) to differentiate between an LCD
        // block and a display on a terminal block, and then we can pass that in to the .GetDrawFunction()

        public partial class TestDisplay : STUDisplay {

            public static float Velocity = 0;

            public static STUDisplayDrawMapper TestDisplayDrawMapper = new STUDisplayDrawMapper();

            public Action<MySpriteDrawFrame, Vector2, float> Drawer;
            public Action<string> Echo;

            public TestDisplay(IMyTextSurface surface, Action<string> echo, string font = "Monospace", float fontSize = 1f) : base(surface, font, fontSize) {

                TestDisplayDrawMapper.Add(STUDisplayType.LCD_PANEL, DrawStandardLCDSprites);
                TestDisplayDrawMapper.Add(STUDisplayType.WIDE_LCD, DrawWideLCDSprites);
                Echo = echo;
                Surface.BackgroundColor = Color.Black;
                Surface.Script = "";
                //Drawer = TestDisplayDrawMapper.GetDrawFunction(Surface);

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

