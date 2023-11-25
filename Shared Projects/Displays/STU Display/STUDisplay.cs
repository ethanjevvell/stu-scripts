using Sandbox.ModAPI.Ingame;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class STUDisplay {

            public IMyTextSurface Surface { get; set; }
            public RectangleF Viewport { get; set; }
            public Vector2 TopLeft { get; private set; }
            public Vector2 Cursor { get; set; }
            public MySpriteDrawFrame CurrentFrame { get; set; }
            /// <summary>
            /// The background sprite to be drawn on every frame.
            /// Background will be blank and black if not overridden.
            /// </summary>
            public MySpriteCollection BackgroundSprite { get; set; }
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
                BackgroundSprite = new MySpriteCollection();
                Viewport = GetViewport();
                TopLeft = Cursor = Viewport.Position;
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
                Cursor = new Vector2(TopLeft.X, Cursor.Y + LineHeight);
            }

            private float CalculateLineHeight() {
                StringBuilder sb = new StringBuilder("E");
                return Surface.MeasureStringInPixels(sb, Surface.Font, Surface.FontSize).Y;
            }

            public void StartFrame() {
                CurrentFrame = Surface.DrawFrame();

                // Draw background sprite if one is defined
                // The default MySprite is a struct with certain default values,
                // so we can't just check if BackgroundSprite is null.
                // User MUST override this value after instantiating the class to have a custom background.
                if (!BackgroundSprite.Equals(default(MySpriteCollection))) {
                    foreach (MySprite sprite in BackgroundSprite.Sprites) {
                        CurrentFrame.Add(sprite);
                    }
                }
            }

            public void EndAndPaintFrame() {
                CurrentFrame.Dispose();
            }

            public void Clear() {
                CurrentFrame.Dispose();
            }

            public void ResetViewport() {
                Viewport = GetViewport();
            }

            private RectangleF GetViewport() {
                var standardViewport = new RectangleF((Surface.TextureSize - Surface.SurfaceSize) / 2f, Surface.SurfaceSize);
                switch (Surface.DisplayName) {
                    case "Large Display":
                        float offsetPx = 8f;
                        return new RectangleF(
                            new Vector2(standardViewport.Position.X + offsetPx, standardViewport.Position.Y + offsetPx),
                            new Vector2(standardViewport.Width - offsetPx * 2, standardViewport.Height - offsetPx * 2));
                    default:
                        return standardViewport;
                }
            }

        }
    }
}
