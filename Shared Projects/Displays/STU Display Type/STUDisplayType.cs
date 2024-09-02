using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {

        public static class STUDisplayBlock {
            public const string HoloLCDLarge = "HoloLCDLarge";
            public const string HoloLCDSmall = "HoloLCDSmall";
            public const string LargeBlockCorner_LCD_1 = "LargeBlockCorner_LCD_1";
            public const string LargeBlockCorner_LCD_2 = "LargeBlockCorner_LCD_2";
            public const string LargeBlockCorner_LCD_Flat_1 = "LargeBlockCorner_LCD_Flat_1";
            public const string LargeBlockCorner_LCD_Flat_2 = "LargeBlockCorner_LCD_Flat_2";
            public const string LargeCurvedLCDPanel = "LargeCurvedLCDPanel";
            public const string LargeDiagonalLCDPanel = "LargeDiagonalLCDPanel";
            public const string LargeFullBlockLCDPanel = "LargeFullBlockLCDPanel";
            public const string LargeLCDPanel = "LargeLCDPanel";
            public const string LargeLCDPanel3x3 = "LargeLCDPanel3x3";
            public const string LargeLCDPanel5x3 = "LargeLCDPanel5x3";
            public const string LargeLCDPanel5x5 = "LargeLCDPanel5x5";
            public const string LargeLCDPanelWide = "LargeLCDPanelWide";
            public const string LargeTextPanel = "LargeTextPanel";
            public const string SmallBlockCorner_LCD_1 = "SmallBlockCorner_LCD_1";
            public const string SmallBlockCorner_LCD_2 = "SmallBlockCorner_LCD_2";
            public const string SmallBlockCorner_LCD_Flat_1 = "SmallBlockCorner_LCD_Flat_1";
            public const string SmallBlockCorner_LCD_Flat_2 = "SmallBlockCorner_LCD_Flat_2";
            public const string SmallCurvedLCDPanel = "SmallCurvedLCDPanel";
            public const string SmallDiagonalLCDPanel = "SmallDiagonalLCDPanel";
            public const string SmallFullBlockLCDPanel = "SmallFullBlockLCDPanel";
            public const string SmallLCDPanel = "SmallLCDPanel";
            public const string SmallLCDPanelWide = "SmallLCDPanelWide";
            public const string SmallTextPanel = "SmallTextPanel";
            public const string TransparentLCDLarge = "TransparentLCDLarge";
            public const string TransparentLCDSmall = "TransparentLCDSmall";
            public const string LargeProgrammableBlock = "LargeProgrammableBlock";
            public const string LargeProgrammableBlockReskin = "LargeProgrammableBlockReskin"; // automations pb
            public const string SmallProgrammableBlock = "SmallProgrammableBlock";
            public const string SmallProgrammableBlockReskin = "SmallProgrammableBlockReskin"; // automations pb
            public const string BuggyCockpit = "BuggyCockpit";
            public const string CockpitOpen = "CockpitOpen";
            public const string DBSmallBlockFighterCockpit = "DBSmallBlockFighterCockpit";
            public const string LargeBlockCockpit = "LargeBlockCockpit";
            public const string LargeBlockCockpitIndustrial = "LargeBlockCockpitIndustrial";
            public const string OpenCockpitLarge = "OpenCockpitLarge";
            public const string OpenCockpitSmall = "OpenCockpitSmall";
            public const string RoverCockpit = "RoverCockpit";
            public const string SmallBlockCapCockpit = "SmallBlockCapCockpit";
            public const string SmallBlockCockpit = "SmallBlockCockpit";
            public const string SmallBlockCockpitIndustrial = "SmallBlockCockpitIndustrial";
            public const string SmallBlockStandingCockpit = "SmallBlockStandingCockpit";
            public const string SpeederCockpit = "SpeederCockpit";
            public const string SpeederCockpitCompact = "SpeederCockpitCompact";
            public const string LargeBlockConsole = "LargeBlockConsole";
        }

        public static class STUSubDisplay {
            public const string ScreenArea = "ScreenArea";
            public const string LargeDisplay = "LargeDisplay";
            public const string Keyboard = "Keyboard";
            public const string Numpad = "Numpad";
            public const string ProjectionArea = "ProjectionArea";
            public const string TopCenterScreen = "TopCenterScreen";
            public const string TopLeftScreen = "TopLeftScreen";
            public const string TopRightScreen = "TopRightScreen";
            public const string BottomCenterScreen = "BottomCenterScreen";
            public const string BottomLeftScreen = "BottomLeftScreen";
            public const string BottomRightScreen = "BottomRightScreen";
        }

        public class STUDisplayType {

            public static string CreateDisplayIdentifier(string block, string display) {
                var displayName = display.ToString();
                var blockName = block.ToString();
                return $"{blockName}.{displayName}";
            }

            public static string GetDisplayIdentifier(IMyTerminalBlock block, int displayIndex) {
                var tempBlock = block as IMyTextSurfaceProvider;
                var surface = tempBlock.GetSurface(displayIndex);
                var blockName = block.BlockDefinition.SubtypeName;
                var surfaceName = surface.DisplayName.Replace(" ", "");
                return $"{blockName}.{surfaceName}";
            }

        }

    }
}
