using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {
        public class DisplayService
        {

            Action<string> Echo;
            public Dictionary<string, MaterialDictionary> materialDictionaries;
            public ItemType category;
            public IMyBlockGroup subscribers;

            public DisplayService(Dictionary<string, MaterialDictionary> materialsDictionaries, ItemType category, IMyBlockGroup subscribers, Action<string> Echo)
            {
                this.materialDictionaries = materialsDictionaries;
                this.category = category;
                this.subscribers = subscribers;
                this.Echo = Echo;
            }

            public string getMaterialCountsString(ItemType category, bool discrete = false)
            {
                string output = "";
                string categoryName = materialDictionaries[category.ToString()].readableName;
                MaterialDictionary materialDictionary = materialDictionaries[category.ToString()];

                output += categoryName;
                output += "\n-----\n";
                foreach (var material in materialDictionary.materialCounts.Keys)
                {
                    if (discrete)
                    {
                        int quantity = (int)materialDictionary.materialCounts[material];
                        string formattedNumber = quantity.ToString();
                        output += $"{material}: {formattedNumber}\n";
                    }
                    else
                    {
                        double quantity = Math.Round(materialDictionary.materialCounts[material], 2);
                        string formattedNumber = quantity.ToString("N2");
                        output += $"{material}: {formattedNumber} kg\n";
                    }
                }
                output += "\n";
                return output;
            }

            public void publish()
            {
                bool discrete = false;
                if (category == ItemType.MyObjectBuilder_Component)
                {
                    discrete = true;
                }

                List<IMyTerminalBlock> panels = new List<IMyTerminalBlock>();
                subscribers.GetBlocks(panels);
                foreach (IMyTextPanel panel in panels)
                {
                    panel.WriteText(getMaterialCountsString(category, discrete));
                    IMyTerminalBlock block = panel;
                    string displayCategory = category.ToString().Split('_')[1].ToLower();
                    Echo($"Published {displayCategory} stats to {block.DisplayNameText}");
                }
            }

        }
    }
}
