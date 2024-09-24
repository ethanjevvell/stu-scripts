using System;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public class STUImage {

            private List<List<float>> pixelArray;

            // Getters and setters
            #region
            public List<List<float>> PixelArray {
                get { return pixelArray; }
            }
            public uint Width {
                get { return pixelArray != null && pixelArray.Count > 0 ? (uint)pixelArray[0].Count : 0; }
            }

            public uint Height {
                get { return pixelArray != null && pixelArray.Count > 0 ? (uint)pixelArray.Count : 0; }
            }
            #endregion

            public STUImage(List<List<float>> image) {
                SetPixelArray(image);
            }

            public STUImage() {
                pixelArray = new List<List<float>>() {
                    new List<float>()
                };
            }

            public void SetPixelArray(List<List<float>> image) {
                ValidatePixelArray(image);
                pixelArray = image;
            }

            private void ValidatePixelArray(List<List<float>> image) {
                if (image == null) {
                    throw new ArgumentException("Image cannot be null.");
                }
                if (image.Count == 0) {
                    throw new ArgumentException("Image must have at least one row.");
                }
                int width = image[0].Count;
                foreach (List<float> row in image) {
                    if (row.Count != width) {
                        throw new ArgumentException("All rows must be the same length.");
                    }
                }
            }

        }
    }
}
