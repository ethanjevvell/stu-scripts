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

            public string createGasDisplayString()
            {
                string hydrogen = ($"Hydrogen level: {(gasDictionary["Hydrogen"] / hydrogenCapacity) * 100}%");
                string oxygen = ($"Oxygen level: {(gasDictionary["Oxygen"] / oxygenCapacity) * 100}%");
                return hydrogen + "\n" + oxygen;
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
