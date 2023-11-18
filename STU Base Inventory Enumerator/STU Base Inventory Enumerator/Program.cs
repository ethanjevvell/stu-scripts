using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        public enum ItemType
        {
            MyObjectBuilder_Ingot,
            MyObjectBuilder_Ore,
            MyObjectBuilder_Component
        }


        MaterialDictionary ingotDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Ingot, "Ingots");
        MaterialDictionary oreDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Ore, "Ores");
        MaterialDictionary componentDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Component, "Components");

        List<IMyTerminalBlock> inventories = new List<IMyTerminalBlock>();
        Dictionary<string, MaterialDictionary> materialDictionaries = new Dictionary<string, MaterialDictionary>();

        public Program()
        {
            inventories = getInventories();
            materialDictionaries.Add(ingotDictionary.category, ingotDictionary);
            materialDictionaries.Add(oreDictionary.category, oreDictionary);
            materialDictionaries.Add(componentDictionary.category, componentDictionary);
        }

        public class MaterialDictionary
        {
            public Dictionary<string, double> materialCounts = new Dictionary<string, double>();
            public string category;
            public string readableName;

            public MaterialDictionary(ItemType category, string readableName)
            {
                this.category = category.ToString();
                this.readableName = readableName;
            }
        }

        public void displayMaterialCounts(MaterialDictionary materialClass)
        {
            Echo(materialClass.readableName);
            Echo("------");
            foreach (var material in materialClass.materialCounts.Keys)
            {
                Echo($"{material}: {materialClass.materialCounts[material]}");
            }
            Echo("");
        }

        public string getMaterialCountsString()
        {
            string output = "";
            foreach (var dict in materialDictionaries.Keys)
            {
                output += materialDictionaries[dict].readableName;
                output += "\n-----\n";
                foreach (var material in materialDictionaries[dict].materialCounts.Keys)
                {
                    output += $"{material}: {materialDictionaries[dict].materialCounts[material]}\n";
                }
                output += "\n";
            }
            return output;
        }

        public void displayAllMaterialCounts()
        {
            foreach (var dict in materialDictionaries.Keys)
            {
                displayMaterialCounts(materialDictionaries[dict]);
            }
        }

        public List<IMyTerminalBlock> getInventories()
        {
            List<IMyTerminalBlock> inventoryBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(inventoryBlocks, block => block.HasInventory);
            return inventoryBlocks;
        }

        public void countMaterials()
        {
            foreach (var inventory in inventories)
            {
                List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();
                inventory.GetInventory(0).GetItems(inventoryItems, item => materialDictionaries.ContainsKey(item.Type.TypeId));

                foreach (var item in inventoryItems)
                {
                    addItem(item);
                }

                if (inventory.InventoryCount > 1)
                {
                    inventory.GetInventory(1).GetItems(inventoryItems, item => materialDictionaries.ContainsKey(item.Type.TypeId));

                    foreach (var item in inventoryItems)
                    {
                        addItem(item);
                    }
                }
            }
        }

        public void addItem(MyInventoryItem item)
        {
            if (!materialDictionaries[item.Type.TypeId].materialCounts.ContainsKey(item.Type.SubtypeId))
            {
                materialDictionaries[item.Type.TypeId].materialCounts[item.Type.SubtypeId] = 0;
            }
            materialDictionaries[item.Type.TypeId].materialCounts[item.Type.SubtypeId] += (double)item.Amount;
        }

        public void resetMaterialCounts()
        {
            foreach (var dict in materialDictionaries.Keys)
            {
                materialDictionaries[dict].materialCounts.Clear();
            }
        }

        public void Main()
        {
            resetMaterialCounts();
            countMaterials();
            displayAllMaterialCounts();

            IMyTextPanel display = GridTerminalSystem.GetBlockWithName("TEST_LCD") as IMyTextPanel;
            if (display != null)
            {
                var result = display.WriteText(getMaterialCountsString());
                if (result == true)
                {
                    Echo("Success writing");
                }
                else
                {
                    Echo("Error writing");
                }
            }
        }
    }
}
