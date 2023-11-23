using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class LogPublisher
        {
            private const float FontScale = 1f;

            private const string Font = "Monospace";

            List<IMyTerminalBlock> panels = new List<IMyTerminalBlock>();
            Dictionary<IMyTextSurface, RectangleF> surfaceViewports = new Dictionary<IMyTextSurface, RectangleF>();
            IMyBlockGroup subscribers;

            List<StuLog> Logs = new List<StuLog>();

            public LogPublisher(IMyBlockGroup subscribers)
            {
                this.subscribers = subscribers;
                this.subscribers.GetBlocks(panels);
                foreach (IMyTextSurfaceProvider panel in panels)
                {
                    var surface = panel.GetSurface(0);
                    surface.ScriptBackgroundColor = Color.Black;
                    surface.ContentType = ContentType.SCRIPT;
                    var viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
                    surfaceViewports.Add(surface, viewport);
                }
            }

            public void ClearPanels()
            {
                foreach (IMyTextPanel block in panels)
                {
                    block.WriteText("");
                }
            }

            public void DrawLineOfText(ref MySpriteDrawFrame frame, ref RectangleF viewport, IMyTextSurface surface, StuLog log)
            {
                var viewportStart = new Vector2(viewport.Position.X, viewport.Position.Y);
                var senderSprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = log.Sender,
                    Position = viewport.Position,
                    RotationOrScale = FontScale,
                    Color = Color.White,
                    FontId = Font,
                };

                frame.Add(senderSprite);
                StringBuilder sb = new StringBuilder(log.Message);
                Vector2 newPos = surface.MeasureStringInPixels(sb, Font, FontScale);
                viewport.Position.X += viewport.Width - newPos.X;

                var messageSprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = log.Message,
                    Position = viewport.Position,
                    RotationOrScale = FontScale,
                    Color = Color.White,
                    FontId = Font,
                };

                frame.Add(messageSprite);

                // return to left-hand side of screen
                viewport.Position = viewportStart;
                // move to next line
                viewport.Position.Y += newPos.Y;
            }

            public void DrawLogs(ref MySpriteDrawFrame frame, RectangleF viewport, IMyTextSurface surface)
            {
                foreach (var log in Logs)
                {
                    DrawLineOfText(ref frame, ref viewport, surface, log);
                }
                frame.Dispose();
            }

            public void Publish(StuLog newLog)
            {
                Logs.Add(newLog);
                foreach (IMyTextSurface surface in surfaceViewports.Keys)
                {
                    var frame = surface.DrawFrame();
                    DrawLogs(ref frame, surfaceViewports[surface], surface);
                    frame.Dispose();
                }
            }

        }
    }
}
