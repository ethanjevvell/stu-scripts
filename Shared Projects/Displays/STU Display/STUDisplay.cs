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
            public float CharacterWidth { get; private set; }
            public int Lines { get; private set; }

            private bool NeedToCenterSprite;

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
                CharacterWidth = CalculateCharacaterWidth();
                Lines = (int)(ScreenHeight / LineHeight);
                NeedToCenterSprite = true;

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

            private float CalculateCharacaterWidth() {
                StringBuilder sb = new StringBuilder("E");
                return Surface.MeasureStringInPixels(sb, Surface.Font, Surface.FontSize).X;
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

            /// <summary>
            /// Aligns a sprite to the center of its parent sprite.
            /// </summary>
            /// <param name="parentSprite"></param>
            /// <param name="childSprite"></param>
            public void CenterWithinParent(MySprite parentSprite, ref MySprite childSprite) {
                childSprite.Alignment = TextAlignment.CENTER;

                switch (childSprite.Type) {

                    case SpriteType.TEXT:

                        switch (parentSprite.Alignment) {

                            // Parent sprite is aligned by its absolute middle
                            case TextAlignment.CENTER:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X,
                                    parentSprite.Position.Value.Y - (LineHeight / 2f)
                                );
                                return;

                            // Parent sprint is aligned by the middle of its left edge
                            case TextAlignment.LEFT:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X + (parentSprite.Size.Value.X / 2f),
                                    parentSprite.Position.Value.Y - (LineHeight / 2f)
                                );
                                return;

                            // Parent sprite is aligned by the middle of its right edge
                            case TextAlignment.RIGHT:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X - (parentSprite.Size.Value.X / 2f),
                                    parentSprite.Position.Value.Y - (LineHeight / 2f)
                                );
                                return;
                        }

                        break;

                    case SpriteType.TEXTURE:

                        switch (parentSprite.Alignment) {

                            case TextAlignment.CENTER:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X,
                                    parentSprite.Position.Value.Y
                                );
                                return;

                            case TextAlignment.LEFT:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X + (parentSprite.Size.Value.X / 2f),
                                    parentSprite.Position.Value.Y
                                );
                                return;

                            case TextAlignment.RIGHT:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X - (parentSprite.Size.Value.X / 2f),
                                    parentSprite.Position.Value.Y
                                );
                                return;

                        }

                        break;
                }

            }

            /// <summary>
            /// Aligns a sprite to the left of its parent sprite, with optional padding.
            /// </summary>
            /// <param name="parentSprite"></param>
            /// <param name="childSprite"></param>
            /// <param name="padding"></param>
            public void AlignLeftWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                if (NeedToCenterSprite) {
                    CenterWithinParent(parentSprite, ref childSprite);
                }

                switch (childSprite.Type) {

                    case SpriteType.TEXT:
                        float stringWidth = childSprite.Data.Length * CharacterWidth;
                        childSprite.Position -= new Vector2(((parentSprite.Size.Value.X - stringWidth) / 2f) - padding, 0);
                        break;

                    case SpriteType.TEXTURE:
                        childSprite.Position -= new Vector2(((parentSprite.Size.Value.X - childSprite.Size.Value.X) / 2f) - padding, 0);
                        break;

                }

            }

            /// <summary>
            /// Aligns a sprite to the right of its parent sprite, with optional padding.
            /// </summary>
            /// <param name="parentSprite"></param>
            /// <param name="childSprite"></param>
            /// <param name="padding"></param>
            public void AlignRightWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                if (NeedToCenterSprite) {
                    CenterWithinParent(parentSprite, ref childSprite);
                }

                switch (childSprite.Type) {

                    case SpriteType.TEXT:
                        float stringWidth = childSprite.Data.Length * CharacterWidth;
                        childSprite.Position += new Vector2(((parentSprite.Size.Value.X - stringWidth) / 2f) - padding, 0);
                        break;

                    case SpriteType.TEXTURE:
                        childSprite.Position += new Vector2(((parentSprite.Size.Value.X - childSprite.Size.Value.X) / 2f) - padding, 0);
                        break;

                }

            }

            /// <summary>
            /// Aligns a sprite to the top of its parent sprite, with optional padding.
            /// </summary>
            /// <param name="parentSprite"></param>
            /// <param name="childSprite"></param>
            /// <param name="padding"></param>
            public void AlignTopWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                if (NeedToCenterSprite) {
                    CenterWithinParent(parentSprite, ref childSprite);
                }

                switch (childSprite.Type) {

                    case SpriteType.TEXT:
                        childSprite.Position -= new Vector2(0, ((parentSprite.Size.Value.Y - LineHeight) / 2f) - padding);
                        break;

                    case SpriteType.TEXTURE:
                        childSprite.Position -= new Vector2(0, ((parentSprite.Size.Value.Y - childSprite.Size.Value.Y) / 2f) - padding);
                        break;

                }

            }

            /// <summary>
            /// Aligns a sprite to the bottom of its parent sprite, with optional padding
            /// </summary>
            /// <param name="parentSprite"></param>
            /// <param name="childSprite"></param>
            /// <param name="padding"></param>
            public void AlignBottomWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                if (NeedToCenterSprite) {
                    CenterWithinParent(parentSprite, ref childSprite);
                }

                switch (childSprite.Type) {

                    case SpriteType.TEXT:
                        childSprite.Position += new Vector2(0, ((parentSprite.Size.Value.Y - LineHeight) / 2f) - padding);
                        break;

                    case SpriteType.TEXTURE:
                        childSprite.Position += new Vector2(0, ((parentSprite.Size.Value.Y - childSprite.Size.Value.Y) / 2f) - padding);
                        break;

                }

            }

            public void AlignTopLeftWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                CenterWithinParent(parentSprite, ref childSprite);
                NeedToCenterSprite = false;
                AlignTopWithinParent(parentSprite, ref childSprite, padding);
                AlignLeftWithinParent(parentSprite, ref childSprite, padding);
                NeedToCenterSprite = true;
            }

            public void AlignTopRightWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                CenterWithinParent(parentSprite, ref childSprite);
                NeedToCenterSprite = false;
                AlignTopWithinParent(parentSprite, ref childSprite, padding);
                AlignRightWithinParent(parentSprite, ref childSprite, padding);
                NeedToCenterSprite = true;
            }

            public void AlignBottomLeftWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                CenterWithinParent(parentSprite, ref childSprite);
                NeedToCenterSprite = false;
                AlignBottomWithinParent(parentSprite, ref childSprite, padding);
                AlignLeftWithinParent(parentSprite, ref childSprite, padding);
                NeedToCenterSprite = true;
            }

            public void AlignBottomRightWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                CenterWithinParent(parentSprite, ref childSprite);
                NeedToCenterSprite = false;
                AlignBottomWithinParent(parentSprite, ref childSprite, padding);
                AlignRightWithinParent(parentSprite, ref childSprite, padding);
                NeedToCenterSprite = true;
            }

        }
    }
}
