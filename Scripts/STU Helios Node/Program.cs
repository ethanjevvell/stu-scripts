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
        Dictionary<string, string> metadata = new Dictionary<string, string>();

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

            metadata.Add("nodeName", nodeName);

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

            // START METADATA CREATION

            metadata.Add("solarPanelOutput", solarPanelOutput.ToString());
            metadata.Add("gatlingAmmoBoxes", gatlingAmmoBoxes.ToString());

            string[] brokenPanelNames = new string[brokenPanels.Count];
            for (int i = 0; i < brokenPanels.Count; i++) {
                Echo(brokenPanels[i].DisplayNameText + " is broken");
                brokenPanelNames[i] = brokenPanels[i].DisplayNameText;
            }
            metadata.Add("brokenPanels", $"{string.Join(",", brokenPanelNames)}");

            foreach (KeyValuePair<string, string> entry in metadata) {
                Echo(entry.Key + ": " + entry.Value);
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
