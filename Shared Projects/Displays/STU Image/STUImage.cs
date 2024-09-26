using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngameScript {
    partial class Program {
        public class STUImage {

            public struct Pixel {
                public float distanceVal;
            }

            private List<List<Pixel>> pixelArray;
            private IEnumerator<bool> exportStateMachine;
            private bool finishedExporting;

            // Getters and setters
            #region
            public List<List<Pixel>> PixelArray {
                get { return pixelArray; }
                set {
                    if (value.Count == 0) {
                        pixelArray = new List<List<Pixel>>() { };
                    } else {
                        int width = value[0].Count;
                        for (int i = 0; i < width; i++) {
                            if (value[i].Count != width) {
                                throw new ArgumentException("All rows must have the same number of columns.");
                            }
                        }
                    }
                }
            }
            public uint Width {
                get { return pixelArray != null && pixelArray.Count > 0 ? (uint)pixelArray[0].Count : 0; }
            }
            public uint Height {
                get { return pixelArray != null && pixelArray.Count > 0 ? (uint)pixelArray.Count : 0; }
            }
            public IEnumerator<bool> ExportStateMachine {
                get { return exportStateMachine; }
                private set { exportStateMachine = value; }
            }
            public bool FinishedExporting {
                get { return finishedExporting; }
                private set { finishedExporting = value; }
            }
            #endregion

            public STUImage(List<List<Pixel>> image) {
                pixelArray = image;
            }

            public STUImage() {
                pixelArray = new List<List<Pixel>>() { };
            }

            public void ExportOverTime(IMyTerminalBlock outputBlock) {
                if (ExportStateMachine != null) {
                    bool hasMoreSteps = ExportStateMachine.MoveNext();
                    if (!hasMoreSteps) {
                        ExportStateMachine.Dispose();
                        ExportStateMachine = null;
                    }
                } else {
                    ExportStateMachine = RunExportCoroutine(outputBlock);
                }
            }

            private IEnumerator<bool> RunExportCoroutine(IMyTerminalBlock outputBlock) {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append('[');
                for (int i = 0; i < Height; i++) {
                    stringBuilder.Append('[');
                    for (int j = 0; j < Width; j++) {
                        stringBuilder.Append(pixelArray[i][j].distanceVal);
                        stringBuilder.Append(",");
                    }
                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    stringBuilder.Append("],");
                    stringBuilder.Append("\n");
                    yield return true;
                }
                stringBuilder.Append(']');
                outputBlock.CustomData = stringBuilder.ToString();
                FinishedExporting = true;
            }

        }
    }
}
