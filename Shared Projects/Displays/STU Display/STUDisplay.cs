using Sandbox.ModAPI.Ingame;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class STUDisplay {

            public IMyTextSurface Surface { get; set; }
            public RectangleF Viewport { get; set; }
            public MySpriteDrawFrame CurrentFrame { get; set; }
            public float ScreenWidth { get; private set; }
            public float ScreenHeight { get; private set; }
            public float LineHeight { get; private set; }
            public int Lines { get; private set; }

            /// <summary>
            /// Custom STU wrapper for text surfaces.
            /// Initializes an LCD with the given font and font size.
            /// Extend this class to create your own display with custom methods / properties.
            /// If not specified, the default font is Monospace and the default font size is 1.
            /// </summary>
            /// <param name="surface"></param>
            /// <param name="font"></param>
            /// <param name="fontSize"></param>
            public STUDisplay(IMyTextSurface surface, string font = "Monospace", float fontSize = 1f) {
                Surface = surface;
                Surface.ContentType = ContentType.SCRIPT;
                Surface.ScriptBackgroundColor = Color.Black;
                Surface.FontSize = fontSize;
                Surface.Font = font;
                Viewport = new RectangleF((Surface.TextureSize - Surface.SurfaceSize) / 2f, Surface.SurfaceSize);
                ScreenWidth = Viewport.Width;
                ScreenHeight = Viewport.Height;
                LineHeight = CalculateLineHeight();
                Lines = (int)(ScreenHeight / LineHeight);

                Clear();
            }

            /// <summary>
            /// Moves the viewport to the next line, where the distance to the next line is the line height.
            /// Line height is calculated by measuring the height of a single character in the display's given font.
            /// Use monospace for best results.
            /// </summary>
            public void GoToNextLine() {
                Viewport = new RectangleF(
                    new Vector2(Viewport.Position.X, Viewport.Position.Y + LineHeight),
                    Viewport.Size);
            }

            private float CalculateLineHeight() {
                StringBuilder sb = new StringBuilder("E");
                Vector2 stringDimensions = Surface.MeasureStringInPixels(sb, Surface.Font, Surface.FontSize);
                return stringDimensions.Y;
            }

            public void StartFrame() {
                CurrentFrame = Surface.DrawFrame();
            }

            public void EndAndPaintFrame() {
                CurrentFrame.Dispose();
            }

            public void Clear() {
                StartFrame();
                EndAndPaintFrame();
            }

        }
    }
}
