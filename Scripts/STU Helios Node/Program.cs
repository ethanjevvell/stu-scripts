using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    partial class Program : MyGridProgram {

        string nodeName;

        STUMasterLogBroadcaster masterLogBroadcaster;
        STULog outgoingLog = new STULog();

        List<IMyTerminalBlock> solarPanels = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> brokenPanels = new List<IMyTerminalBlock>();

        List<IMyTerminalBlock> storageBlocks = new List<IMyTerminalBlock>();

        // Measured in megawatts
        float solarPanelOutput;
        int gatlingAmmoBoxes;

        public Program() {

            masterLogBroadcaster = new STUMasterLogBroadcaster("HELIOS_MASTER_NODE", IGC, TransmissionDistance.AntennaRelay);
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solarPanels);
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(storageBlocks);

            nodeName = Me.CustomData;

            if (solarPanels == null) {
                throw new Exception("No solar panels found");
            }

            if (storageBlocks == null) {
                throw new Exception("No storage blocks found");
            }

            if (string.IsNullOrEmpty(nodeName)) {
                throw new Exception("No slave node name found");
            }

        }

        public void Main() {

            solarPanelOutput = 0;
            gatlingAmmoBoxes = 0;

            InspectSolarPanels();
            InspectInventories();

            Echo("Total solar panel output: " + solarPanelOutput + " MW");
            Echo("Number of broken panels: " + brokenPanels.Count);
            Echo("Number of gatling ammo boxes: " + gatlingAmmoBoxes);

            if (brokenPanels.Count > 0) {
                Echo("WARNING: Broken panel(s) detected");
                outgoingLog.Sender = nodeName;
                outgoingLog.Message = "Broken solar panels detected: " + brokenPanels.Count;
                outgoingLog.Type = STULogType.WARNING;
                masterLogBroadcaster.Log(outgoingLog);
            }

        }

        public void InspectInventories() {
            foreach (IMyCargoContainer storage in storageBlocks) {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                storage.GetInventory().GetItems(items);

                foreach (MyInventoryItem item in items) {
                    if (item.Type.SubtypeId.ToString() == "NATO_25x184mm") {
                        gatlingAmmoBoxes += item.Amount.ToIntSafe();
                    }
                }
            }
        }

        public void InspectSolarPanels() {

            foreach (IMySolarPanel panel in solarPanels) {

                // Check for broken panels
                if (!panel.IsFunctional) {
                    brokenPanels.Add(panel);
                }

                // Get the output of the solar panels
                solarPanelOutput += panel.CurrentOutput;

            }
        }

    }
}
