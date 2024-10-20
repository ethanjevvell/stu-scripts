using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class STUDisplay {

            public IMyTextSurface Surface { get; private set; }
            public RectangleF Viewport { get; private set; }
            public Vector2 TopLeft { get; private set; }
            public Vector2 Center { get; private set; }
            public float Center_X { get; private set; }
            public float Center_Y { get; private set; }
            public Vector2 Cursor { get; set; }
            public MySpriteDrawFrame CurrentFrame { get; set; }
            /// <summary>
            /// The background sprite to be drawn on every frame.
            /// Background will be blank and black if not overridden.
            /// </summary>
            public MySpriteCollection BackgroundSprite { get; set; }
            public float ScreenWidth { get; private set; }
            public float ScreenHeight { get; private set; }
            public float DefaultLineHeight { get; set; }
            public float CharacterWidth { get; private set; }
            public int Lines { get; set; }

            IEnumerator<bool> ImageDrawerStateMachine { get; set; }
            public bool FinishedDrawingCustomImage { get; private set; }

            /// <summary>
            /// Used to determine if a sprite needs to be centered within its parent sprite.
            /// Flag intended for internal use; do not modify unless you know what you're doing.
            /// </summary>
            private bool NeedToCenterSprite;

            public static Dictionary<string, RectangleF> ViewportOffsets { get; set; } = new Dictionary<string, RectangleF>
            {
                {"Large Display", new RectangleF(new Vector2(8, 8), new Vector2(512, 320))},
                {"Keyboard", new RectangleF(new Vector2(0, 48), new Vector2(512, 204.8f))},
                {"Bottom Left Screen", new RectangleF(new Vector2(60, 0), new Vector2(192, 256)) },
            };

            /// <summary>
            /// Custom STU wrapper for text surfaces.
            /// Initializes an LCD with the given font and font size.
            /// Extend this class to create your own display with custom methods / properties.
            /// If not specified, the default font is Monospace and the default font size is 1.
            /// </summary>
            /// <param name="surface"></param>
            /// <param name="font"></param>
            /// <param name="fontSize"></param>
            public STUDisplay(IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1f) {
                var tempBlock = block as IMyTextSurfaceProvider;
                Surface = TryGetSurface(tempBlock, displayIndex);
                Surface.Script = "";
                Surface.ContentType = ContentType.SCRIPT;
                Surface.ScriptBackgroundColor = Color.Black;
                Surface.FontSize = fontSize;
                Surface.Font = font;
                BackgroundSprite = new MySpriteCollection();
                Viewport = GetViewport();
                TopLeft = Cursor = Viewport.Position;
                Center = Viewport.Center;
                ScreenWidth = Viewport.Width;
                ScreenHeight = Viewport.Height;
                DefaultLineHeight = GetDefaultLineHeight();
                Lines = (int)(ScreenHeight / DefaultLineHeight);
                NeedToCenterSprite = true;
                FinishedDrawingCustomImage = false;
                Clear();
            }

            /// <summary>
            /// Moves the viewport to the next line, where the distance to the next line is the line height.
            /// Line height is calculated by measuring the height of a single character in font you provided in the constructor.
            /// Monospace is the default font.
            /// </summary>
            public void GoToNextLine(float verticalPadding = 0) {
                Cursor = new Vector2(TopLeft.X, Cursor.Y + DefaultLineHeight + verticalPadding);
            }

            public void StartFrame() {
                CurrentFrame = Surface.DrawFrame();

                // Draw background sprite if one is defined
                // The default MySprite is a struct with certain default values,
                // so we can't just check if BackgroundSprite is null.
                // User MUST override this value to have a custom background.
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

            private IMyTextSurface TryGetSurface(IMyTextSurfaceProvider tempBlock, int displayIndex) {
                var surface = tempBlock.GetSurface(displayIndex);
                if (surface == null) {
                    throw new Exception($"Failed to get display at index {displayIndex}; double-check the index of the display you're trying to write to.");
                }
                return surface;
            }

            private RectangleF GetViewport() {
                // the following line of code returns the coordinates of the top left corner of the actual writable area
                // WITH RESPECT TO the top left corner of the texture
                // this is important because whenever we define the position of a sprite, the coordinates that get read are with respect to the TEXTURE, not the viewable area (suface)
                var standardViewport = new RectangleF((Surface.TextureSize - Surface.SurfaceSize) / 2f, Surface.SurfaceSize);
                return standardViewport;
            }

            private IEnumerator<bool> RunDrawCustomImageCoroutine(STUImage image, uint width, uint height, double minDistance, double maxDistance, Action<string> echo) {
                StartFrame();
                float pixelSideLength = ScreenWidth / width;
                for (int i = 0; i < height; i++) {
                    for (int j = 0; j < width; j++) {
                        float distanceData = image.PixelArray[i][j].distanceVal;
                        Color pixelColor = GetPixelColorFromDistance(distanceData, minDistance, maxDistance);
                        MySprite pixel = new MySprite() {
                            Type = SpriteType.TEXTURE,
                            Data = "SquareSimple",
                            // Remember that the middle-left side of a sprite is where the position is anchored, so we shift the position by half the pixel side length
                            // This assumes that pixels are always square
                            Position = new Vector2(j * pixelSideLength, i * pixelSideLength + pixelSideLength / 2),
                            Size = new Vector2(pixelSideLength, pixelSideLength),
                            Color = pixelColor
                        };
                        CurrentFrame.Add(pixel);
                        // Yield every 512 pixels; arbitrary but conservative to prevent script complexity timeout
                        if ((i * height + j) % 512 == 0) {
                            yield return true;
                        }
                    }
                }
                EndAndPaintFrame();
            }

            private Color GetPixelColorFromDistance(float distance, double minDistance, double maxDistance) {
                double redIntensity;
                double blueIntensity;
                if (distance == -1) {
                    redIntensity = 0;
                    blueIntensity = 0;
                } else {
                    double normalizedDistance = (distance - minDistance) / (maxDistance - minDistance);
                    normalizedDistance = Math.Max(0, Math.Min(1, normalizedDistance));
                    redIntensity = 1 - normalizedDistance;
                    blueIntensity = normalizedDistance;
                }
                byte redValue = (byte)(redIntensity * 255);
                byte blueValue = (byte)(blueIntensity * 255);
                return new Color(redValue, 0, blueValue);
            }

            public void DrawCustomImageOverTime(STUImage image, uint width, uint height, double minDistance, double maxDistance, Action<string> echo) {
                if (ImageDrawerStateMachine != null && !FinishedDrawingCustomImage) {
                    bool hasMoreSteps = ImageDrawerStateMachine.MoveNext();
                    if (!hasMoreSteps) {
                        echo("disposing of drawer state machine");
                        ImageDrawerStateMachine.Dispose();
                        ImageDrawerStateMachine = null;
                        FinishedDrawingCustomImage = true;
                    }
                } else {
                    ImageDrawerStateMachine = RunDrawCustomImageCoroutine(image, width, height, minDistance, maxDistance, echo);
                }
            }

            private float GetTextSpriteWidth(MySprite sprite) {
                StringBuilder builder = new StringBuilder();
                return Surface.MeasureStringInPixels(builder.Append(sprite.Data), sprite.FontId, sprite.RotationOrScale).X;
            }

            public float GetTextSpriteWidth(string s, float scale = 1f, string fontID = "Monospace") {
                StringBuilder builder = new StringBuilder();
                builder.Append(s);
                return Surface.MeasureStringInPixels(builder, fontID, scale).X;
            }

            private float GetTextSpriteHeight(MySprite sprite) {
                StringBuilder builder = new StringBuilder();
                return Surface.MeasureStringInPixels(builder.Append(sprite.Data), sprite.FontId, sprite.RotationOrScale).Y;
            }

            public float GetTextSpriteHeight(string s, float scale = 1f, string fontID = "Monospace") {
                StringBuilder builder = new StringBuilder();
                builder.Append(s);
                return Surface.MeasureStringInPixels(builder, fontID, scale).Y;
            }

            private float GetDefaultLineHeight() {
                StringBuilder builder = new StringBuilder();
                return Surface.MeasureStringInPixels(builder.Append("A"), Surface.Font, Surface.FontSize).Y;
            }

            /// <summary>
            /// Aligns a sprite to the center of its parent sprite.
            /// </summary>
            /// <param name="parentSprite"></param>
            /// <param name="childSprite"></param>
            public void AlignCenterWithinParent(MySprite parentSprite, ref MySprite childSprite) {

                childSprite.Alignment = TextAlignment.CENTER;

                switch (childSprite.Type) {

                    case SpriteType.TEXT:

                        var textSpriteLineHeight = GetTextSpriteHeight(childSprite);

                        switch (parentSprite.Alignment) {

                            // Parent sprite is aligned by its absolute middle
                            case TextAlignment.CENTER:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X,
                                    parentSprite.Position.Value.Y - (textSpriteLineHeight / 2f)
                                );
                                return;

                            // Parent sprint is aligned by the middle of its left edge
                            case TextAlignment.LEFT:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X + (parentSprite.Size.Value.X / 2f),
                                    parentSprite.Position.Value.Y - (textSpriteLineHeight / 2f)
                                );
                                return;

                            // Parent sprite is aligned by the middle of its right edge
                            case TextAlignment.RIGHT:
                                childSprite.Position = new Vector2(
                                    parentSprite.Position.Value.X - (parentSprite.Size.Value.X / 2f),
                                    parentSprite.Position.Value.Y - (textSpriteLineHeight / 2f)
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
                    AlignCenterWithinParent(parentSprite, ref childSprite);
                }

                switch (childSprite.Type) {

                    case SpriteType.TEXT:
                        childSprite.Position -= new Vector2(((parentSprite.Size.Value.X - GetTextSpriteWidth(childSprite)) / 2f) - padding, 0);
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
                    AlignCenterWithinParent(parentSprite, ref childSprite);
                }

                switch (childSprite.Type) {

                    case SpriteType.TEXT:
                        childSprite.Position += new Vector2(((parentSprite.Size.Value.X - GetTextSpriteWidth(childSprite)) / 2f) - padding, 0);
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
                    AlignCenterWithinParent(parentSprite, ref childSprite);
                }

                switch (childSprite.Type) {

                    case SpriteType.TEXT:
                        childSprite.Position -= new Vector2(0, ((parentSprite.Size.Value.Y - GetTextSpriteHeight(childSprite)) / 2f) - padding);
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
                    AlignCenterWithinParent(parentSprite, ref childSprite);
                }

                switch (childSprite.Type) {

                    case SpriteType.TEXT:
                        childSprite.Position += new Vector2(0, ((parentSprite.Size.Value.Y - GetTextSpriteHeight(childSprite)) / 2f) - padding);
                        break;

                    case SpriteType.TEXTURE:
                        childSprite.Position += new Vector2(0, ((parentSprite.Size.Value.Y - childSprite.Size.Value.Y) / 2f) - padding);
                        break;

                }

            }

            public void AlignTopLeftWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                AlignCenterWithinParent(parentSprite, ref childSprite);
                NeedToCenterSprite = false;
                AlignTopWithinParent(parentSprite, ref childSprite, padding);
                AlignLeftWithinParent(parentSprite, ref childSprite, padding);
                NeedToCenterSprite = true;
            }

            public void AlignTopRightWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                AlignCenterWithinParent(parentSprite, ref childSprite);
                NeedToCenterSprite = false;
                AlignTopWithinParent(parentSprite, ref childSprite, padding);
                AlignRightWithinParent(parentSprite, ref childSprite, padding);
                NeedToCenterSprite = true;
            }

            public void AlignBottomLeftWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                AlignCenterWithinParent(parentSprite, ref childSprite);
                NeedToCenterSprite = false;
                AlignBottomWithinParent(parentSprite, ref childSprite, padding);
                AlignLeftWithinParent(parentSprite, ref childSprite, padding);
                NeedToCenterSprite = true;
            }

            public void AlignBottomRightWithinParent(MySprite parentSprite, ref MySprite childSprite, float padding = 0) {
                AlignCenterWithinParent(parentSprite, ref childSprite);
                NeedToCenterSprite = false;
                AlignBottomWithinParent(parentSprite, ref childSprite, padding);
                AlignRightWithinParent(parentSprite, ref childSprite, padding);
                NeedToCenterSprite = true;
            }

            public void WriteWrappableLogs(Queue<STULog> logs, Func<STULog, string> logFormatter = null) {
                // If there are no logs, don't bother writing anything
                if (logs.Count == 0) {
                    return;
                }
                Cursor = TopLeft;
                if (logFormatter == null) {
                    logFormatter = DefaultLogFormatter;
                }
                // Count the number of lines the logs will take up
                int logLines = 0;
                foreach (STULog log in logs) {
                    logLines += GetLinesConsumed(logFormatter(log));
                }

                // Dequeue logs until the number of lines is less than the number of lines the display can show
                while (logLines > Lines) {
                    STULog log = logs.Dequeue();
                    logLines -= GetLinesConsumed(logFormatter(log));
                }

                foreach (STULog log in logs) {
                    StringBuilder logSegment = new StringBuilder();
                    foreach (char c in logFormatter(log)) {
                        logSegment.Append(c);
                        if (GetTextWidth(logSegment) >= ScreenWidth) {
                            // Remove the last character from the segment
                            logSegment.Remove(logSegment.Length - 1, 1);
                            CreateLogSprite(log, logSegment);
                            GoToNextLine();
                            logSegment.Clear();
                            // Be sure to re-add the character that was removed, excepting whitespace
                            if (c != ' ') {
                                logSegment.Append(c);
                            }
                        }
                    }

                    // Add the last segment
                    CreateLogSprite(log, logSegment);
                    GoToNextLine();
                }
            }

            private string DefaultLogFormatter(STULog log) {
                return $" > {log.Sender}: {log.Message}";
            }

            private void CreateLogSprite(STULog log, StringBuilder logSegment) {
                CurrentFrame.Add(new MySprite() {
                    Type = SpriteType.TEXT,
                    Data = logSegment.ToString(),
                    Position = Cursor,
                    RotationOrScale = Surface.FontSize,
                    Color = STULog.GetColor(log.Type),
                    FontId = Surface.Font,
                });
            }

            private float GetTextWidth(StringBuilder segment) {
                return Surface.MeasureStringInPixels(segment, Surface.Font, Surface.FontSize).X;
            }

            private float GetTextWidth(string s) {
                return GetTextWidth(new StringBuilder().Append(s));
            }

            private int GetLinesConsumed(string text) {
                return (int)Math.Ceiling(GetTextWidth(text) / ScreenWidth);
            }

        }
    }
}
