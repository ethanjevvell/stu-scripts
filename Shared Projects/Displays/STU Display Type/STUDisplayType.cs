using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {

        public enum STUDisplayBlock {
            HoloLCDLarge,
            HoloLCDSmall,
            LargeBlockCorner_LCD_1,
            LargeBlockCorner_LCD_2,
            LargeBlockCorner_LCD_Flat_1,
            LargeBlockCorner_LCD_Flat_2,
            LargeCurvedLCDPanel,
            LargeDiagonalLCDPanel,
            LargeFullBlockLCDPanel,
            LargeLCDPanel,
            LargeLCDPanel3x3,
            LargeLCDPanel5x3,
            LargeLCDPanel5x5,
            LargeLCDPanelWide,
            LargeTextPanel,
            SmallBlockCorner_LCD_1,
            SmallBlockCorner_LCD_2,
            SmallBlockCorner_LCD_Flat_1,
            SmallBlockCorner_LCD_Flat_2,
            SmallCurvedLCDPanel,
            SmallDiagonalLCDPanel,
            SmallFullBlockLCDPanel,
            SmallLCDPanel,
            SmallLCDPanelWide,
            SmallTextPanel,
            TransparentLCDLarge,
            TransparentLCDSmall,
            LargeProgrammableBlock,
            LargeProgrammableBlockReskin, // automations pb
            SmallProgrammableBlock,
            SmallProgrammableBlockReskin, // automations pb
            BuggyCockpit,
            CockpitOpen,
            DBSmallBlockFighterCockpit,
            LargeBlockCockpit,
            LargeBlockCockpitIndustrial,
            OpenCockpitLarge,
            OpenCockpitSmall,
            RoverCockpit,
            SmallBlockCapCockpit,
            SmallBlockCockpit,
            SmallBlockCockpitIndustrial,
            SmallBlockStandingCockpit,
            SpeederCockpit,
            SpeederCockpitCompact,
            LargeBlockConsole,
        }
        public enum STUSubDisplay {
            ScreenArea,
            LargeDisplay,
            Keyboard,
            Numpad,
            ProjectionArea,
            TopCenterScreen,
            TopLeftScreen,
            TopRightScreen,
            BottomCenterScreen,
            BottomLeftScreen,
            BottomRightScreen,
        }

        public class STUDisplayType {

            public static string CreateDisplayIdentifier(STUDisplayBlock block, STUSubDisplay display) {
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
