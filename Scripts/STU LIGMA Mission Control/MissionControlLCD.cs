using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class MissionControlLCD : STUDisplay {

            public Queue<STULog> FlightLogs { get; set; }
            public double Velocity { get; set; }
            public double CurrentFuel { get; set; }
            public double CurrentPower { get; set; }

            public MissionControlLCD(IMyTextSurface surface, string font = "Monospace", float fontSize = 1) : base(surface, font, fontSize) {

                FlightLogs = new Queue<STULog>();
                Velocity = 0;
                CurrentFuel = 0;
                CurrentPower = 0;

                // create background sprite
                BackgroundSprite = new MySpriteCollection {
                    Sprites = new MySprite[] {
                        // ADD SPRITES HERE
                    }
                };
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

            public void DrawLogs(STULog latestLog) {
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

            public void DrawTelemetryData(STULog log) {
                try {
                    Velocity = double.Parse(log.Metadata["Velocity"]);
                    CurrentFuel = double.Parse(log.Metadata["CurrentFuel"]);
                    CurrentPower = double.Parse(log.Metadata["CurrentPower"]);
                } catch {
                    Velocity = -1;
                    CurrentFuel = -1;
                    CurrentPower = -1;
                }
                // CurrentFrame.Add(CreateVelocitySprite());
                // CurrentFrame.Add(CreateCurrentFuelSprite());
                // CurrentFrame.Add(CreateCurrentPowerSprite());
                var parentSprite = new MySprite() {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Color = Color.Green,
                    Size = new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
                    Position = Viewport.Center,
                    Alignment = TextAlignment.RIGHT
                };

                var childSprite = new MySprite() {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Size = new Vector2(ScreenHeight * 0.2f, ScreenWidth * 0.7f),
                    Color = Color.White,
                };

                var childTextSprite = new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = $"Velocity: {Velocity} m/s",
                    Position = new Vector2(0, 0),
                    RotationOrScale = Surface.FontSize,
                    Color = Color.Black,
                    FontId = Surface.Font,
                };

                AlignBottomWithinParent(parentSprite, ref childTextSprite, 10);

                CurrentFrame.Add(parentSprite);
                CurrentFrame.Add(childTextSprite);
            }

            public MySprite CreateVelocitySprite() {
                return new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = $"Velocity: {Velocity} m/s",
                    Position = TopLeft + new Vector2(ScreenWidth / 2f, (ScreenHeight / 2f) - LineHeight / 2f),
                    RotationOrScale = Surface.FontSize,
                    Alignment = TextAlignment.CENTER,
                    Color = Color.White,
                    FontId = Surface.Font,
                };
            }


            public MySprite CreateCurrentFuelSprite() {
                return new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = $"Current fuel: {CurrentFuel} L",
                    Position = new Vector2(10, 0),
                    RotationOrScale = Surface.FontSize,
                    Color = Color.White,
                    FontId = Surface.Font,
                };
            }

            public MySprite CreateCurrentPowerSprite() {
                return new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = $"Current power: {CurrentPower} kWh",
                    Position = new Vector2(30, 70),
                    RotationOrScale = Surface.FontSize,
                    Color = Color.White,
                    FontId = Surface.Font,
                };
            }
        }
    }
}
