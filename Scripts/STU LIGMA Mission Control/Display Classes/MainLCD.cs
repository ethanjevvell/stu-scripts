using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class MainLCD : STUDisplay {

            public Queue<STULog> FlightLogs { get; set; }
            public static double Velocity { get; set; }
            public static double CurrentFuel { get; set; }
            public static double CurrentPower { get; set; }

            private Action<MySpriteDrawFrame, Vector2, float> Drawer;
            private static STUDisplayDrawMapper MainLCDMapper = new STUDisplayDrawMapper {
                DisplayDrawMapper = {
                    { STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.LargeLCDPanelWide, STUSubDisplay.ScreenArea), LargeWideLCD.ScreenArea },
                }
            };

            public MainLCD(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize) {

                FlightLogs = new Queue<STULog>();
                Velocity = 0;
                CurrentFuel = 0;
                CurrentPower = 0;
                Drawer = MainLCDMapper.GetDrawFunction(block, displayIndex);

            }

            public string FormatLog(STULog log) => $" > {log.Sender}: {log.Message} ";

            public void DrawLineOfText(STULog log) {

                var sprite = new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = FormatLog(log),
                    Position = Cursor,
                    RotationOrScale = Surface.FontSize,
                    Color = STULog.GetColor(log.Type),
                    FontId = Surface.Font,
                };

                CurrentFrame.Add(sprite);
                GoToNextLine();
            }

            private void DrawLogs(STULog latestLog) {
                Cursor = TopLeft;

                // Scroll effect implemented with a queue
                if (FlightLogs.Count > Lines) {
                    FlightLogs.Dequeue();
                }

                // Draw the logs
                foreach (var log in FlightLogs) {
                    DrawLineOfText(log);
                }

                // Telemetry data only comes from the most recent log
                DrawTelemetryData(latestLog);
            }

            private void DrawTelemetryData(STULog log) {
                try {
                    Velocity = double.Parse(log.Metadata["Velocity"]);
                    CurrentFuel = double.Parse(log.Metadata["CurrentFuel"]);
                    CurrentPower = double.Parse(log.Metadata["CurrentPower"]);
                } catch {
                    Velocity = -1;
                    CurrentFuel = -1;
                    CurrentPower = -1;
                }
                Drawer.Invoke(CurrentFrame, Viewport.Center, 1f);
            }

            public void UpdateDisplay(STULog latestLog) {
                StartFrame();
                DrawLogs(latestLog);
                EndAndPaintFrame();
            }
        }
    }
}
