using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {
        public class GasDisplayService
        {
            private Action<string> Echo;
            public Dictionary<string, double> gasDictionary;
            public IMyBlockGroup subscribers;
            public List<IMyTerminalBlock> panels = new List<IMyTerminalBlock>();

            double hydrogenCapacity;
            double oxygenCapacity;

            public GasDisplayService(Dictionary<string, double> gasDictionary, IMyBlockGroup subscribers, double hydrogenCapacity, double oxygenCapacity, Action<string> Echo)
            {
                this.gasDictionary = gasDictionary;
                this.subscribers = subscribers;
                this.hydrogenCapacity = hydrogenCapacity;
                this.oxygenCapacity = oxygenCapacity;
                this.Echo = Echo;

                subscribers.GetBlocks(panels);
            }

            private string formatQuantity(double quantity)
            {
                if (quantity >= 1000000)
                {
                    return $"{quantity / 1000000:F2}m L";
                }
                else if (quantity >= 10000 && quantity <= 999999)
                {
                    return $"{quantity / 1000:F1}k L";
                }
                else if (quantity >= 1000 && quantity <= 9999)
                {
                    return $"{Math.Round(quantity)} L";
                }
                else
                {
                    return $"{quantity:F2} L";
                }
            }

            public string createGasDisplayString()
            {
                string output = "";

                double hydrogenLevel = (gasDictionary["Hydrogen"] / hydrogenCapacity) * 100;
                double oxygenLevel = (gasDictionary["Oxygen"] / oxygenCapacity) * 100;

                output += "BASE GASSES\n";
                output += "-------------\n\n";
                output += $"h2: [{getGasStatusBar(hydrogenLevel)}] {hydrogenLevel}%\n";
                output += $"o2: [{getGasStatusBar(oxygenLevel)}] {oxygenLevel}%\n";
                output += "\n";
                output += $"Total h2 capacity: {formatQuantity(hydrogenCapacity)}\n";
                output += $"Total o2 capacity: {formatQuantity(oxygenCapacity)}\n";
                return output;
            }

            public string getGasStatusBar(double level)
            {
                int bars = (int)level / 3;
                int spaces = 33 - bars;
                return new string('|', bars) + new string(' ', spaces);
            }

            public void publish()
            {
                string outputString = createGasDisplayString();
                foreach (IMyTextPanel panel in panels)
                {
                    panel.WriteText(outputString);
                    Echo($"Published gas stats to {panel.DisplayNameText}");
                }
            }
        }
    }
}
