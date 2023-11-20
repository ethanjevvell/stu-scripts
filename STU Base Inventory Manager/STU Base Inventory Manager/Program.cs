
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
            MyObjectBuilder_Component,
            MyObjectBuilder_GasProperties,
            INVALID_ITEM_TYPE
        }

        IMyBlockGroup ingotSubscribers;
        IMyBlockGroup oreSubscribers;
        IMyBlockGroup componentSubscribers;

        SimpleDisplayService ingotDisplayService;
        SimpleDisplayService oreDisplayService;
        SimpleDisplayService componentDisplayService;

        MaterialDictionary ingotDictionary;
        MaterialDictionary oreDictionary;
        MaterialDictionary componentDictionary;

        List<IMyTerminalBlock> inventories = new List<IMyTerminalBlock>();
        Dictionary<ItemType, MaterialDictionary> materialDictionaries = new Dictionary<ItemType, MaterialDictionary>();

        public Program()
        {
            getInventories(inventories);

            // NOTE: Initiating subscribers in the constructor means that the script
            // will need to be recompiled every time the user wants to enroll a new
            // LCD in the display service

            ingotSubscribers = GridTerminalSystem.GetBlockGroupWithName("INGOT_LCDS");
            oreSubscribers = GridTerminalSystem.GetBlockGroupWithName("ORE_LCDS");
            componentSubscribers = GridTerminalSystem.GetBlockGroupWithName("COMPONENT_LCDS");

            ingotDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Ingot, "Ingots");
            oreDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Ore, "Ores");
            componentDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Component, "Components");

            materialDictionaries.Add(ItemType.MyObjectBuilder_Ingot, ingotDictionary);
            materialDictionaries.Add(ItemType.MyObjectBuilder_Ore, oreDictionary);
            materialDictionaries.Add(ItemType.MyObjectBuilder_Component, componentDictionary);

            ingotDisplayService = new SimpleDisplayService(ingotDictionary, ingotSubscribers, Echo);
            oreDisplayService = new SimpleDisplayService(oreDictionary, oreSubscribers, Echo);
            componentDisplayService = new SimpleDisplayService(componentDictionary, componentSubscribers, Echo);

            // Script will run every 100 ticks
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public class MaterialDictionary
        {
            public Dictionary<string, double> materialCounts = new Dictionary<string, double>();
            public ItemType category;
            public string readableName;

            public MaterialDictionary(ItemType category, string readableName)
            {
                this.category = category;
                this.readableName = readableName;
            }
        }

        public void getInventories(List<IMyTerminalBlock> inventories)
        {
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(inventories, block => block.HasInventory);
        }

        public void countMaterials()
        {
            foreach (var inventory in inventories)
            {
                List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();
                inventory.GetInventory(0).GetItems(inventoryItems, item => materialDictionaries.ContainsKey(toItemType(item.Type.TypeId)));

                foreach (var item in inventoryItems)
                {
                    addItem(item);
                }

                if (inventory.InventoryCount > 1)
                {
                    inventory.GetInventory(1).GetItems(inventoryItems, item => materialDictionaries.ContainsKey(toItemType(item.Type.TypeId)));

                    foreach (var item in inventoryItems)
                    {
                        addItem(item);
                    }
                }
            }
        }

        public void addItem(MyInventoryItem item)
        {
            ItemType itemType = toItemType(item.Type.TypeId);
            string subType = item.Type.SubtypeId;

            if (!materialDictionaries[itemType].materialCounts.ContainsKey(subType))
            {
                materialDictionaries[itemType].materialCounts[subType] = 0;
            }
            materialDictionaries[itemType].materialCounts[subType] += (double)item.Amount;
        }

        public ItemType toItemType(string s)
        {
            switch (s)
            {
                case "MyObjectBuilder_Ingot":
                    return ItemType.MyObjectBuilder_Ingot;
                case "MyObjectBuilder_Ore":
                    return ItemType.MyObjectBuilder_Ore;
                case "MyObjectBuilder_Component":
                    return ItemType.MyObjectBuilder_Component;
                default:
                    return ItemType.INVALID_ITEM_TYPE;
            }
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
            Echo($"Previous runtime: {Runtime.LastRunTimeMs}");
            resetMaterialCounts();
            countMaterials();

            ingotDisplayService.publish();
            oreDisplayService.publish();
            componentDisplayService.publish();
        }
    }
}
