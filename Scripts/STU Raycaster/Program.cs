using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyCameraBlock Camera;
        IMyTerminalBlock Cockpit;
        STUDisplay CockpitDisplay;
        STURaycaster Raycaster;
        STUMasterLogBroadcaster Broadcaster;

        public const string LIGMA_DATA_STREAM_CHANNEL = "LIGMA_VEHICLE_CONTROL";
        public const string SHIP_NAME = "SDC-3";

        public Program() {
            Broadcaster = new STUMasterLogBroadcaster(LIGMA_DATA_STREAM_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            Cockpit = GridTerminalSystem.GetBlockWithName("Badger 2-R Flight Seat MAIN");
            if (Cockpit == null) {
                throw new Exception("Cockpit not found");
            }
            CockpitDisplay = new STUDisplay(Cockpit, 0);
            CockpitDisplay.Surface.ContentType = ContentType.TEXT_AND_IMAGE;
            CockpitDisplay.Surface.BackgroundColor = Color.Blue;
            Camera = GridTerminalSystem.GetBlockWithName("Raycasting Camera") as IMyCameraBlock;
            if (Camera != null) {
                Raycaster = new STURaycaster(Camera);
                Raycaster.RaycastDistance = 1000;
            }
        }

        public void Main(string argument) {
            ParseCommand(argument);
        }

        public void ParseCommand(string argument) {

            switch (argument) {

                case "Raycast":
                    Raycast();
                    break;

                case "ToggleRaycast":
                    Raycaster.ToggleRaycast();
                    break;
            }
        }

        public void Raycast() {
            try {
                var hit = Raycaster.Raycast();
                if (!hit.IsEmpty()) {
                    var hitInfo = Raycaster.GetHitInfoString(hit);
                    Echo(hitInfo);
                    CockpitDisplay.Surface.WriteText(hitInfo);
                    var metadata = Raycaster.GetHitInfoDictionary(hit);
                    Broadcaster.Log(new STULog {
                        Sender = SHIP_NAME,
                        Message = $"Received metadata: {metadata["Velocity"]}",
                        Type = STULogType.INFO,
                        Metadata = metadata
                    });
                } else {
                    CockpitDisplay.Surface.WriteText("No hit");
                }
            } catch {
                CockpitDisplay.Surface.WriteText("Raycast failed");
            }
        }

    }
}
