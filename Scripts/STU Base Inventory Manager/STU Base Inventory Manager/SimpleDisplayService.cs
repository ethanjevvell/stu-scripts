
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public class SimpleDisplayService {
            public MaterialDictionary materialDictionary;
            public IMyBlockGroup subscribers;
            public List<IMyTerminalBlock> panels = new List<IMyTerminalBlock>();
            private Action<string> Echo;

            public SimpleDisplayService(MaterialDictionary materialDictionary, IMyBlockGroup subscribers, Action<string> Echo) {
                this.subscribers = subscribers;
                this.subscribers.GetBlocks(panels);
                this.materialDictionary = materialDictionary;
                this.Echo = Echo;
            }

            public string getMaterialCountsString(bool discrete = false) {
                string output = "";

                output += materialDictionary.readableName;
                output += "\n-----\n";
                foreach (var material in materialDictionary.materialCounts.Keys) {
                    if (discrete) {
                        int quantity = (int)materialDictionary.materialCounts[material];
                        string formattedNumber = formatDiscreteQuantity(quantity);
                        output += $"{material}: {formattedNumber}\n";
                    } else {
                        double quantity = Math.Round(materialDictionary.materialCounts[material], 2);
                        string formattedNumber = formatQuantity(quantity);
                        output += $"{material}: {formattedNumber} kg\n";
                    }
                }
                output += "\n";
                return output;
            }

            private string formatQuantity(double quantity) {
                if (quantity >= 1000000) {
                    return $"{quantity / 1000000:F2}m";
                } else if (quantity >= 10000 && quantity <= 999999) {
                    return $"{quantity / 1000:F1}k";
                } else if (quantity >= 1000 && quantity <= 9999) {
                    return $"{Math.Round(quantity)}";
                } else {
                    return $"{quantity:F2}";
                }
            }

            private string formatDiscreteQuantity(int quantity) {
                if (quantity >= 1000000) {
                    return $"{quantity / 1000000.0:F2}m";
                } else if (quantity >= 10000) {
                    return $"{quantity / 1000.0:F1}k";
                } else {
                    return quantity.ToString();
                }
            }

            public void publish() {
                bool discrete = false;
                if (materialDictionary.category == ItemType.MyObjectBuilder_Component) {
                    discrete = true;
                }

                string displayCategory = materialDictionary.category.ToString().Split('_')[1].ToLower();
                foreach (IMyTextPanel panel in panels) {
                    panel.WriteText(getMaterialCountsString(discrete));
                    Echo($"Published {displayCategory} stats to {panel.DisplayNameText}");
                }
            }

        }
    }
}
