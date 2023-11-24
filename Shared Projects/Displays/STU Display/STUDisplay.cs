using Sandbox.ModAPI.Ingame;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class STUDisplay {

            public IMyTextSurface Surface { get; set; }
            public RectangleF Viewport { get; set; }
            public float ScreenWidth { get; set; }
            public float ScreenHeight { get; set; }
            public float LineHeight { get; set; }
            public int Lines { get; set; }

            /// <summary>
            /// A wrapper for a text surface that allows for easy drawing of logs.
            /// Initializes an LCD with the given font and font size.
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
            }

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

        }
    }
}
