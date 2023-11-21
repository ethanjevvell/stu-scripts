
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
            INVALID_ITEM_TYPE
        }

        IMyBlockGroup ingotSubscribers;
        IMyBlockGroup oreSubscribers;
        IMyBlockGroup componentSubscribers;
        IMyBlockGroup gasSubscribers;

        SimpleDisplayService ingotDisplayService;
        SimpleDisplayService oreDisplayService;
        SimpleDisplayService componentDisplayService;

        GasDisplayService gasDisplayService;

        MaterialDictionary ingotDictionary;
        MaterialDictionary oreDictionary;
        MaterialDictionary componentDictionary;

        Dictionary<string, double> gasDictionary;
        double HYDROGEN_CAPACITY = 0;
        double OXYGEN_CAPACITY = 0;

        List<IMyTerminalBlock> INVENTORIES = new List<IMyTerminalBlock>();
        List<IMyGasTank> GAS_TANKS = new List<IMyGasTank>();

        Dictionary<ItemType, MaterialDictionary> materialDictionaries = new Dictionary<ItemType, MaterialDictionary>();

        public Program()
        {
            getInventories();
            getTanks();
            calculateBaseGasCapacities();

            // NOTE: Initiating subscribers in the constructor means that the script
            // will need to be recompiled every time the user wants to enroll a new
            // LCD in the display service

            ingotSubscribers = GridTerminalSystem.GetBlockGroupWithName("INGOT_LCDS");
            oreSubscribers = GridTerminalSystem.GetBlockGroupWithName("ORE_LCDS");
            componentSubscribers = GridTerminalSystem.GetBlockGroupWithName("COMPONENT_LCDS");
            gasSubscribers = GridTerminalSystem.GetBlockGroupWithName("GAS_LCDS");

            ingotDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Ingot, "Ingots");
            oreDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Ore, "Ores");
            componentDictionary = new MaterialDictionary(ItemType.MyObjectBuilder_Component, "Components");

            gasDictionary = new Dictionary<string, double>()
            {
                { "Hydrogen", 0 },
                { "Oxygen", 0 }
            };

            materialDictionaries.Add(ItemType.MyObjectBuilder_Ingot, ingotDictionary);
            materialDictionaries.Add(ItemType.MyObjectBuilder_Ore, oreDictionary);
            materialDictionaries.Add(ItemType.MyObjectBuilder_Component, componentDictionary);
            // We do not want gas dictionary here; it is not a "material"

            ingotDisplayService = new SimpleDisplayService(ingotDictionary, ingotSubscribers, Echo);
            oreDisplayService = new SimpleDisplayService(oreDictionary, oreSubscribers, Echo);
            componentDisplayService = new SimpleDisplayService(componentDictionary, componentSubscribers, Echo);

            gasDisplayService = new GasDisplayService(gasDictionary, gasSubscribers, HYDROGEN_CAPACITY, OXYGEN_CAPACITY, Echo);

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

        public void getInventories()
        {
            GridTerminalSystem.GetBlocksOfType(INVENTORIES, block => block.HasInventory);
        }

        public void getTanks()
        {
            GridTerminalSystem.GetBlocksOfType(GAS_TANKS, tank => tank.CubeGrid == Me.CubeGrid);
        }

        public void calculateBaseGasCapacities()
        {
            foreach (var tank in GAS_TANKS)
            {
                if (tank.BlockDefinition.SubtypeName.Contains("Hydrogen"))
                {
                    HYDROGEN_CAPACITY += tank.Capacity;
                }
                else if (tank.BlockDefinition.ToString().Contains("Oxygen"))
                {
                    OXYGEN_CAPACITY += tank.Capacity;
                }
            }

        }

        public void countMaterials()
        {
            foreach (var inventory in INVENTORIES)
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


        public void measureGas()
        {
            foreach (var tank in GAS_TANKS)
            {
                double capacity = tank.Capacity;
                double filledRatio = tank.FilledRatio;
                double quantity = filledRatio * capacity;

                if (tank.BlockDefinition.SubtypeName.Contains("Hydrogen"))
                {
                    addGas("Hydrogen", quantity);
                }
                else if (tank.BlockDefinition.ToString().Contains("Oxygen"))
                {
                    addGas("Oxygen", quantity);
                }
            }
        }

        public void addGas(string gas, double quantity)
        {
            if (!gasDictionary.ContainsKey(gas))
            {
                gasDictionary[gas] = 0;
            }
            gasDictionary[gas] += quantity;
        }

        public void clearGasMeasurements()
        {
            gasDictionary.Clear();
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
            Echo($"Previous runtime: {Runtime.LastRunTimeMs} ms");
            resetMaterialCounts();
            clearGasMeasurements();

            countMaterials();
            measureGas();

            ingotDisplayService.publish();
            oreDisplayService.publish();
            componentDisplayService.publish();
            gasDisplayService.publish();
        }
    }
}
