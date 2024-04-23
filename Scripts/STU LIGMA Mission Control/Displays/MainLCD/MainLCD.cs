using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class MainLCD : STUDisplay {

            public static double VelocityMagnitude { get; set; }
            public static Vector3D VelocityComponents { get; set; }
            public static Vector3D AccelerationComponents { get; set; }
            public static double CurrentFuel { get; set; }
            public static double CurrentPower { get; set; }
            public static double FuelCapacity { get; set; }
            public static double PowerCapacity { get; set; }
            public static Vector3D CurrentPosition { get; set; }

            private Action<MySpriteDrawFrame, Vector2, float> Drawer;

            private static STUDisplayDrawMapper MainLCDMapper = new STUDisplayDrawMapper {
                DisplayDrawMapper = {
                    { STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.LargeLCDPanelWide, STUSubDisplay.ScreenArea), LargeWideLCD.ScreenArea },
                }
            };

            public MainLCD(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {
                VelocityComponents = Vector3D.Zero;
                FuelCapacity = 0;
                CurrentFuel = 0;
                CurrentPower = 0;
                PowerCapacity = 0;
                Drawer = MainLCDMapper.GetDrawFunction(block, displayIndex);
            }

            private void ParseTelemetryData(STULog log) {

                if (log.Metadata == null) {
                    return;
                }

                try {
                    Vector3D parsedVelocity;
                    Vector3D parsedAcceleration;
                    VelocityComponents = Vector3D.TryParse(log.Metadata["VelocityComponents"], out parsedVelocity) ? parsedVelocity : Vector3D.Zero;
                    AccelerationComponents = Vector3D.TryParse(log.Metadata["AccelerationComponents"], out parsedAcceleration) ? parsedAcceleration : Vector3D.Zero;
                    VelocityMagnitude = double.Parse(log.Metadata["VelocityMagnitude"]);
                    CurrentFuel = double.Parse(log.Metadata["CurrentFuel"]);
                    CurrentPower = double.Parse(log.Metadata["CurrentPower"]);
                    FuelCapacity = double.Parse(log.Metadata["FuelCapacity"]);
                    PowerCapacity = double.Parse(log.Metadata["PowerCapacity"]);
                } catch {
                    VelocityComponents = Vector3D.Zero;
                    AccelerationComponents = Vector3D.Zero;
                    VelocityMagnitude = 69;
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
