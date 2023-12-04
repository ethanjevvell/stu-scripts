using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class MainLCD : STUDisplay {

            public static double Velocity { get; set; }
            public static double CurrentFuel { get; set; }
            public static double CurrentPower { get; set; }
            public static double FuelCapacity { get; set; }
            public static double PowerCapacity { get; set; }
            public static Vector3D CurrentPosition { get; set; }

            private Action<MySpriteDrawFrame, Vector2, float> Drawer;
            private MyMovingAverage VelocityMovingAverage;

            private static STUDisplayDrawMapper MainLCDMapper = new STUDisplayDrawMapper {
                DisplayDrawMapper = {
                    { STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.LargeLCDPanelWide, STUSubDisplay.ScreenArea), LargeWideLCD.ScreenArea },
                }
            };

            public MainLCD(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {
                Velocity = 0;
                CurrentFuel = 0;
                CurrentPower = 0;
                Drawer = MainLCDMapper.GetDrawFunction(block, displayIndex);
                VelocityMovingAverage = new MyMovingAverage(10);
            }

            private void ParseTelemetryData(STULog log) {
                try {
                    Velocity = double.Parse(log.Metadata["Velocity"]);
                    CurrentFuel = double.Parse(log.Metadata["CurrentFuel"]);
                    CurrentPower = double.Parse(log.Metadata["CurrentPower"]);
                    FuelCapacity = double.Parse(log.Metadata["FuelCapacity"]);
                    PowerCapacity = double.Parse(log.Metadata["PowerCapacity"]);

                    // Display velocity as a moving average for smoother display experience
                    VelocityMovingAverage.Enqueue((float)Velocity);
                    Velocity = VelocityMovingAverage.Avg;
                } catch {
                    Velocity = 69;
                    CurrentFuel = 69;
                    CurrentPower = 100;
                    FuelCapacity = 100;
                    PowerCapacity = 100;
                }
            }

            private void DrawTelemetryData() {
                Drawer.Invoke(CurrentFrame, Viewport.Center, 1f);
            }

            public void UpdateDisplay(STULog latestLog) {
                ParseTelemetryData(latestLog);
                StartFrame();
                DrawTelemetryData();
                EndAndPaintFrame();
            }
        }
    }
}
