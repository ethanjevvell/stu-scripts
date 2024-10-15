using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class STUInventoryEnumerator {

            const string c = "MyObjectBuilder_Component";
            const string g = "MyObjectBuilder_GasProperties";
            const string i = "MyObjectBuilder_Ingot";
            const string o = "MyObjectBuilder_Ore";
            const string co = "MyObjectBuilder_ConsumableItem";
            const string d = "MyObjectBuilder_Datapad";
            const string p = "MyObjectBuilder_Package";
            const string po = "MyObjectBuilder_PhysicalObject";
            const string pg = "MyObjectBuilder_PhysicalGunObject";
            const string gc = "MyObjectBuilder_GasContainerObject";
            const string oc = "MyObjectBuilder_OxygenContainerObject";
            const string am = "MyObjectBuilder_AmmoMagazine";

            public static Dictionary<string, string> SubtypeToNameDict = new Dictionary<string, string>() {
                // Components
                { c + "/BulletproofGlass", "Bulletproof Glass" },
                { c + "/Canvas", "Canvas" },
                { c + "/Computer", "Computer" },
                { c + "/Construction", "Construction Comp." },
                { c + "/Detector", "Detector Comp." },
                { c + "/Display", "Display" },
                { c + "/EngineerPlushie", "Engineer Plushie" },
                { c + "/Explosives", "Explosives" },
                { c + "/Girder", "Girder" },
                { c + "/GravityGenerator", "Gravity Comp." },
                { c + "/InteriorPlate", "Interior Plate" },
                { c + "/LargeTube", "Large Steel Tube" },
                { c + "/Medical", "Medical Comp." },
                { c + "/MetalGrid", "Metal Grid" },
                { c + "/Motor", "Motor" },
                { c + "/PowerCell", "Power Cell" },
                { c + "/RadioCommunication", "Radio-comm Comp." },
                { c + "/Reactor", "Reactor Comp." },
                { c + "/SabiroidPlushie", "Saberoid Plushie" },
                { c + "/SmallTube", "Small Steel Tube" },
                { c + "/SolarCell", "Solar Cell" },
                { c + "/SteelPlate", "Steel Plate" },
                { c + "/Superconductor", "Superconductor" },
                { c + "/Thrust", "Thruster Comp." },
                { c + "/ZoneChip", "Zone Chip" },

                // Gas
                { g + "/Hydrogen", "Hydrogen" },
                { g + "/Oxygen", "Oxygen" },

                // Ingots
                { i + "/Cobalt", "Cobalt Ingot" },
                { i + "/Gold", "Gold Ingot" },
                { i + "/Stone", "Gravel" },
                { i + "/Iron", "Iron Ingot" },
                { i + "/Magnesium", "Magnesium Powder" },
                { i + "/Nickel", "Nickel Ingot" },
                { i + "/Scrap", "Old Scrap Metal" },
                { i + "/Platinum", "Platinum Ingot" },
                { i + "/Silicon", "Silicon Wafer" },
                { i + "/Silver", "Silver Ingot" },
                { i + "/Uranium", "Uranium Ingot" },

                // Ores
                { o + "/Cobalt", "Cobalt Ore" },
                { o + "/Gold", "Gold Ore" },
                { o + "/Ice", "Ice" },
                { o + "/Iron", "Iron Ore" },
                { o + "/Magnesium", "Magnesium Ore" },
                { o + "/Nickel", "Nickel Ore" },
                { o + "/Organic", "Organic" },
                { o + "/Platinum", "Platinum Ore" },
                { o + "/Scrap", "Scrap Metal" },
                { o + "/Silicon", "Silicon Ore" },
                { o + "/Silver", "Silver Ore" },
                { o + "/Stone", "Stone" },
                { o + "/Uranium", "Uranium Ore" },

                // Other
                { co + "/ClangCola", "Clang Kola" },
                { co + "/CosmicCoffee", "Cosmic Coffee" },
                { d + "/Datapad", "Datapad" },
                { co + "/Medkit", "Medkit" },
                { p + "/Package", "Package" },
                { co + "/Powerkit", "Powerkit" },
                { po + "/SpaceCredit", "Space Credit" },


                // Ammo
                { am + "/NATO_5p56x45mm", "5.56x45mm NATO magazine" },
                { am + "/LargeCalibreAmmo", "Artillery Shell" },
                { am + "/MediumCalibreAmmo", "Assault Cannon Shell" },
                { am + "/AutocannonClip", "Autocannon Magazine" },
                { am + "/FireworksBoxBlue", "Fireworks Blue" },
                { am + "/FireworksBoxGreen", "Fireworks Green" },
                { am + "/FireworksBoxPink", "Fireworks Pink" },
                { am + "/FireworksBoxRainbow", "Fireworks Rainbow" },
                { am + "/FireworksBoxRed", "Fireworks Red" },
                { am + "/FireworksBoxYellow", "Fireworks Yellow" },
                { am + "/FlareClip", "Flare Gun Clip" },
                { am + "/NATO_25x184mm", "Gatling Ammo Box" },
                { am + "/LargeRailgunAmmo", "Large Railgun Sabot" },
                { am + "/AutomaticRifleGun_Mag_20rd", "MR-20 Rifle Magazine" },
                { am + "/UltimateAutomaticRifleGun_Mag_30rd", "MR-30E Rifle Magazine" },
                { am + "/RapidFireAutomaticRifleGun_Mag_50rd", "MR-50A Rifle Magazine" },
                { am + "/PreciseAutomaticRifleGun_Mag_5rd", "MR-8P Rifle Magazine" },
                { am + "/Missile200mm", "Rocket" },
                { am + "/SemiAutoPistolMagazine", "S-10 Pistol Magazine" },
                { am + "/ElitePistolMagazine", "S-10E Pistol Magazine" },
                { am + "/FullAutoPistolMagazine", "S-20A Pistol Magazine" },
                { am + "/SmallRailgunAmmo", "Small Railgun Sabot" },

                // Tools
                { pg + "/AngleGrinder4Item", "Elite Grinder" },
                { pg + "/HandDrill4Item", "Elite Hand Drill" },
                { pg + "/Welder4Item", "Elite Welder" },
                { pg + "/AngleGrinder2Item", "Enhanced Grinder" },
                { pg + "/HandDrill2Item", "Enhanced Hand Drill" },
                { pg + "/Welder2Item", "Enhanced Welder" },
                { pg + "/FlareGunItem", "Flare Gun" },
                { pg + "/AngleGrinderItem", "Grinder" },
                { pg + "/HandDrillItem", "Hand Drill" },
                { gc + "/HydrogenBottle", "Hydrogen Bottle" },
                { pg + "/AutomaticRifleItem", "MR-20 Rifle" },
                { pg + "/UltimateAutomaticRifleItem", "MR-30E Rifle" },
                { pg + "/RapidFireAutomaticRifleItem", "MR-50A Rifle" },
                { pg + "/PreciseAutomaticRifleItem", "MR-8P Rifle" },
                { oc + "/OxygenBottle", "Oxygen Bottle" },
                { pg + "/AdvancedHandHeldLauncherItem", "PRO-1 Rocket Launcher" },
                { pg + "/AngleGrinder3Item", "Proficient Grinder" },
                { pg + "/HandDrill3Item", "Proficient Hand Drill" },
                { pg + "/Welder3Item", "Proficient Welder" },
                { pg + "/BasicHandHeldLauncherItem", "RO-1 Rocket Launcher" },
                { pg + "/SemiAutoPistolItem", "S-10 Pistol" },
                { pg + "/ElitePistolItem", "S-10E Pistol" },
                { pg + "/FullAutoPistolItem", "S-20A Pistol" },
                { pg + "/WelderItem", "Welder" }
            };

            List<IMyInventory> Inventories = new List<IMyInventory>();
            List<IMyGasTank> Tanks = new List<IMyGasTank>();
            List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();

            public float HydrogenCapacity { get; private set; } = 0;
            public float OxygenCapacity { get; private set; } = 0;
            public float PowerCapacity { get; private set; } = 0;
            public float StorageCapacity { get; private set; } = 0;

            Dictionary<string, double> RunningItemTotals = new Dictionary<string, double>();
            Dictionary<string, double> MostRecentItemTotals = new Dictionary<string, double>();

            IEnumerator<bool> InventoryEnumeratorStateMachine;
            float InventoryIndex;

            public STUInventoryEnumerator(IMyGridTerminalSystem grid, IMyProgrammableBlock me) {
                List<IMyTerminalBlock> gridBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocks(gridBlocks);
                Inventories = GetInventories(gridBlocks, me);
                grid.GetBlocksOfType(Tanks, (block) => block.CubeGrid == me.CubeGrid);
                grid.GetBlocksOfType(Batteries, (block) => block.CubeGrid == me.CubeGrid);
                Init();
            }

            public STUInventoryEnumerator(IMyGridTerminalSystem grid, List<IMyTerminalBlock> blocks, IMyProgrammableBlock me) {
                Inventories = GetInventories(blocks, me);
                grid.GetBlocksOfType(Batteries);
                grid.GetBlocksOfType(Tanks);
                Init();
            }

            public STUInventoryEnumerator(List<IMyTerminalBlock> blocks, List<IMyGasTank> tanks, List<IMyBatteryBlock> batteries, IMyProgrammableBlock me) {
                Inventories = GetInventories(blocks, me);
                Tanks = tanks;
                Batteries = batteries;
                Init();
            }

            void Init() {
                HydrogenCapacity = GetHydrogenCapacity();
                OxygenCapacity = GetOxygenCapacity();
                PowerCapacity = GetPowerCapacity();
                StorageCapacity = GetStorageCapacity();
            }

            List<IMyInventory> GetInventories(List<IMyTerminalBlock> blocks, IMyProgrammableBlock me) {
                List<IMyInventory> outputList = new List<IMyInventory>();
                foreach (IMyTerminalBlock block in blocks) {
                    if (block.HasInventory && block.CubeGrid == me.CubeGrid) {
                        for (int j = 0; j < block.InventoryCount; j++) {
                            outputList.Add(block.GetInventory(j));
                        }
                    }
                }
                return outputList;
            }

            public void EnumerateInventories() {

                if (InventoryEnumeratorStateMachine == null) {
                    InventoryEnumeratorStateMachine = EnumerateInventoriesCoroutine(Inventories, Tanks, Batteries).GetEnumerator();
                    // Clear the item totals if we're starting a new enumeration
                    RunningItemTotals.Clear();
                    InventoryIndex = 0;
                }

                // Process inventories incrementally
                if (InventoryEnumeratorStateMachine.MoveNext()) {
                    return;
                }

                InventoryEnumeratorStateMachine.Dispose();
                InventoryEnumeratorStateMachine = null;

            }

            IEnumerable<bool> EnumerateInventoriesCoroutine(List<IMyInventory> inventories, List<IMyGasTank> tanks, List<IMyBatteryBlock> batteries) {

                var items = new List<MyInventoryItem>();

                foreach (IMyInventory inventory in inventories) {
                    items.Clear();
                    inventory.GetItems(items);
                    foreach (MyInventoryItem item in items) {
                        ProcessItem(item);
                    }
                    InventoryIndex++;
                    yield return true;
                }

                foreach (IMyGasTank tank in tanks) {
                    ProcessTank(tank);
                    InventoryIndex++;
                    yield return true;
                }

                foreach (IMyBatteryBlock battery in batteries) {
                    ProcessBattery(battery);
                    InventoryIndex++;
                    yield return true;
                }

                MostRecentItemTotals = RunningItemTotals;

            }

            void ProcessItem(MyInventoryItem item) {
                string itemName = item.Type.TypeId + "/" + item.Type.SubtypeId;
                if (SubtypeToNameDict.ContainsKey(itemName)) {
                    if (RunningItemTotals.ContainsKey(SubtypeToNameDict[itemName])) {
                        RunningItemTotals[SubtypeToNameDict[itemName]] += (double)item.Amount;
                    } else {
                        RunningItemTotals[SubtypeToNameDict[itemName]] = (double)item.Amount;
                    }
                } else {
                    throw new System.Exception($"Unknown item: \n {item.Type.TypeId} \n {item.Type.SubtypeId} \n {item.Type.ToString()}");
                }
            }

            void ProcessTank(IMyGasTank tank) {

                string tankType = tank.BlockDefinition.SubtypeId;

                if (tankType.Contains("Oxygen")) {
                    double oxygenInLiters = (double)tank.Capacity * (double)tank.FilledRatio;
                    if (RunningItemTotals.ContainsKey("Oxygen")) {
                        RunningItemTotals["Oxygen"] += oxygenInLiters;
                    } else {
                        RunningItemTotals["Oxygen"] = oxygenInLiters;
                    }
                } else if (tankType.Contains("Hydrogen")) {
                    double hydrogenInLiters = (double)tank.Capacity * (double)tank.FilledRatio;
                    if (RunningItemTotals.ContainsKey("Hydrogen")) {
                        RunningItemTotals["Hydrogen"] += hydrogenInLiters;
                    } else {
                        RunningItemTotals["Hydrogen"] = hydrogenInLiters;
                    }
                } else {
                    throw new System.Exception($"Unknown tank: \n {tank.BlockDefinition.TypeId} \n {tank.BlockDefinition.SubtypeId} \n {tank.BlockDefinition.ToString()}");
                }
            }

            void ProcessBattery(IMyBatteryBlock battery) {
                double powerInWattHours = (double)battery.CurrentStoredPower;
                if (RunningItemTotals.ContainsKey("Power")) {
                    RunningItemTotals["Power"] += powerInWattHours;
                } else {
                    RunningItemTotals["Power"] = powerInWattHours;
                }
            }

            float GetHydrogenCapacity() {
                float totalCapacity = 0;
                foreach (IMyGasTank tank in Tanks) {
                    if (tank.BlockDefinition.SubtypeId.Contains("Hydrogen")) {
                        totalCapacity += tank.Capacity;
                    }
                }
                return totalCapacity;
            }

            float GetOxygenCapacity() {
                float totalCapacity = 0;
                foreach (IMyGasTank tank in Tanks) {
                    if (tank.BlockDefinition.SubtypeId.Contains("Oxygen")) {
                        totalCapacity += tank.Capacity;
                    }
                }
                return totalCapacity;
            }

            float GetPowerCapacity() {
                float totalCapacity = 0;
                foreach (IMyBatteryBlock battery in Batteries) {
                    totalCapacity += battery.MaxStoredPower;
                }
                return totalCapacity;
            }

            public Dictionary<string, double> GetItemTotals() {
                return MostRecentItemTotals;
            }

            public float GetProgress() {
                // Ensure we don't divide by zero
                int totalCount = Inventories.Count + Tanks.Count + Batteries.Count;
                if (totalCount == 0)
                    return 1;

                return InventoryIndex / totalCount;
            }

            float GetStorageCapacity() {
                return Inventories.ToArray().Sum(inventory => (float)inventory.MaxVolume);
            }

            public float GetFilledRatio() {
                double currentOccupiedVolume = Inventories.ToArray().Sum(inventory => (double)inventory.CurrentVolume);
                return (float)(currentOccupiedVolume / StorageCapacity);
            }

        }
    }
}
