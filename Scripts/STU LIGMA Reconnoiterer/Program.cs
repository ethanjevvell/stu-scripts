using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyTerminalBlock Cockpit;
        STUDisplay CockpitDisplay;
        STUDisplay ImageDisplay;
        STURaycaster Raycaster;
        STUMasterLogBroadcaster Broadcaster;
        MyCommandLine CommandLineParser;

        public Dictionary<string, Action> ProgramCommands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        public Program() {

            Cockpit = GetMainCockpit();
            CockpitDisplay = InitCockpitDisplay(Cockpit);
            ImageDisplay = InitImageDisplay(Cockpit);
            Raycaster = InitRaycaster();
            Raycaster.RaycastDistance = 10000;
            Broadcaster = new STUMasterLogBroadcaster(LIGMA_VARIABLES.LIGMA_RECONNOITERER_BROADCASTER, IGC, TransmissionDistance.AntennaRelay);

            CommandLineParser = new MyCommandLine();
            ProgramCommands.Add("Raycast", Raycast);
            ProgramCommands.Add("ToggleRaycast", Raycaster.ToggleRaycast);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;

        }

        public void Main(string argument) {
            ParseCommand(argument);
        }

        public void ParseCommand(string argument) {

            if (CommandLineParser.TryParse(argument)) {
                Action commandAction;
                string commandString = CommandLineParser.Argument(0);

                if (string.IsNullOrEmpty(commandString)) {
                    Echo("No command specified.");
                    return;
                }

                if (ProgramCommands.TryGetValue(commandString, out commandAction)) {
                    commandAction();
                    return;
                }

                Echo($"Command {commandString} not recognized.\n");
                Echo("Available commands:\n");
                foreach (var command in ProgramCommands.Keys) {
                    Echo($"\t{command}\n");
                }
            }

            float distance = 10;
            float fov = 45;
            uint width = 80;
            uint height = 80;

            double minDistance = 0.1;
            double maxDistance = distance;

            try {
                Echo($"Estimated wait: {width * height * distance / 2000 / 60} mins");
                Raycaster.TakeImageOverTime(distance, fov, width, height, Echo);
                Echo($"width = {width}, height = {height}");
                Echo($"Image size: {Raycaster.Image.Width} x {Raycaster.Image.Height}");
                Echo($"Progress: {(float)Raycaster.Image.Height / height * 100}%");
                if (Raycaster.FinishedTakingImage) {
                    Echo("Drawing...");
                    ImageDisplay.DrawCustomImageOverTime(Raycaster.Image, width, height, minDistance, maxDistance, Echo);
                    if (ImageDisplay.FinishedDrawingCustomImage) {
                        Echo("Exporting...");
                        Raycaster.Image.ExportOverTime(Raycaster.Camera);
                        if (Raycaster.Image.FinishedExporting) {
                            Echo("Done");
                        }
                    }
                }
            } catch (Exception e) {
                Echo("Error in image capture: " + e.Message);
            }

        }

        public void Raycast() {
            try {
                var hit = Raycaster.Raycast();
                if (!hit.IsEmpty()) {
                    var hitInfo = Raycaster.GetHitInfoString(hit);
                    var metadata = Raycaster.GetHitInfoDictionary(hit);
                    CockpitDisplay.Surface.WriteText(hitInfo);
                    Echo(hitInfo);

                    string coordinates = metadata["Position"];
                    string coordinateString = FormatCoordinates(coordinates);
                    string broadcastString = $"Target spotted at {coordinateString}";

                    Broadcaster.Log(new STULog {
                        Sender = LIGMA_VARIABLES.LIGMA_RECONNOITERER_NAME,
                        Message = broadcastString,
                        Type = STULogType.INFO,
                        Metadata = metadata
                    });

                } else {
                    CockpitDisplay.Surface.WriteText("No hit");
                }
            } catch {
                var outString = $"Raycast failed \n" +
                    $"Available range = {Raycaster.Camera.AvailableScanRange}";
                Echo(outString);
                CockpitDisplay.Surface.WriteText(outString);
            }
        }

        public IMyTerminalBlock GetMainCockpit() {
            var cockpitBlocks = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(cockpitBlocks);
            foreach (var block in cockpitBlocks) {
                var cockpit = block;
                if (cockpit.IsMainCockpit) {
                    return cockpit;
                }
            }
            throw new Exception("No main cockpit found. Be sure to choose a cockpit and select it as the main cockpit");
        }

        public STUDisplay InitCockpitDisplay(IMyTerminalBlock cockpit) {
            var display = new STUDisplay(cockpit, 0);
            display.Surface.ContentType = ContentType.TEXT_AND_IMAGE;
            display.Surface.BackgroundColor = Color.Blue;
            return display;
        }

        public STURaycaster InitRaycaster() {
            var cameraName = Me.CustomData.Trim();
            var camera = GridTerminalSystem.GetBlockWithName(cameraName) as IMyCameraBlock;
            if (camera != null) {
                return new STURaycaster(camera);
            } else {
                throw new Exception($"No camera found with name {cameraName}. Be sure to enter the name of the camera you want as the raycaster in the PB's Custom Data field");
            }
        }

        public STUDisplay InitImageDisplay(IMyTerminalBlock cockpit) {
            var block = GridTerminalSystem.GetBlockWithName("LogLCD") as IMyTextPanel;
            if (block == null) {
                throw new Exception("No block found with name LogLCD");
            }
            var display = new STUDisplay(block, 0);
            return display;
        }

        public string FormatCoordinates(string coordinateString) {
            var coordinates = coordinateString.Split(' ');
            var x = double.Parse(coordinates[0].Split(':')[1].Trim());
            var y = double.Parse(coordinates[1].Split(':')[1].Trim());
            var z = double.Parse(coordinates[2].Split(':')[1].Trim());
            return $"({x.ToString("0.00")}, {y.ToString("0.00")}, {z.ToString("0.00")})";
        }

    }
}
