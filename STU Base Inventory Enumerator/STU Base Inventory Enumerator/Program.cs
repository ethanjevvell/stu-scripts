using Sandbox.ModAPI.Ingame;
using System;
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

        public void echoMaterialCounts(MaterialDictionary materialClass)
        {
            Echo(materialClass.readableName);
            Echo("------");
            foreach (var material in materialClass.materialCounts.Keys)
            {
                int quantity = (int)Math.Round(materialClass.materialCounts[material]);
                Echo($"{material}: {quantity} kg");
            }
            Echo("");
        }

        public void echoAllMaterialCounts()
        {
            foreach (var dict in materialDictionaries.Keys)
            {
                echoMaterialCounts(materialDictionaries[dict]);
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

            IMyBlockGroup ingotSubscribers = GridTerminalSystem.GetBlockGroupWithName("INGOT_LCDS");
            IMyBlockGroup oreSubscribers = GridTerminalSystem.GetBlockGroupWithName("ORE_LCDS");
            IMyBlockGroup componentSubscribers = GridTerminalSystem.GetBlockGroupWithName("COMPONENT_LCDS");
            IMyBlockGroup test = GridTerminalSystem.GetBlockGroupWithName("COMPONENT_LCDS");

            DisplayService ingotDisplayService = new DisplayService(materialDictionaries, ItemType.MyObjectBuilder_Ingot, ingotSubscribers, Echo);
            DisplayService oreDisplayService = new DisplayService(materialDictionaries, ItemType.MyObjectBuilder_Ore, oreSubscribers, Echo);
            DisplayService componentService = new DisplayService(materialDictionaries, ItemType.MyObjectBuilder_Component, componentSubscribers, Echo);

            ingotDisplayService.publish();
            oreDisplayService.publish();
            componentService.publish();
        }
    }
}
