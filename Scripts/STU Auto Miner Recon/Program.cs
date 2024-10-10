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
        STURaycaster Raycaster;
        STUMasterLogBroadcaster JobBroadcaster;
        MyCommandLine CommandLineParser;

        public Dictionary<string, Action> ProgramCommands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        public Program() {

            Cockpit = GetMainCockpit();
            CockpitDisplay = InitCockpitDisplay(Cockpit);
            Raycaster = InitRaycaster();
            Raycaster.RaycastDistance = 10000;
            JobBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_RECON_JOB_LISTENER, IGC, TransmissionDistance.AntennaRelay);

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

        }

        public void Raycast() {
            try {

                PlaneD jobPlane = ScanJobPlane();
                Vector3D jobSite = ScanJobSite();

                CockpitDisplay.Surface.WriteText(jobPlane.ToString());
                JobBroadcaster.Log(new STULog {
                    Sender = AUTO_MINER_VARIABLES.AUTO_MINER_RECON_NAME,
                    Message = "Transmitting new job site",
                    Type = STULogType.INFO,
                    Metadata = new Dictionary<string, string> {
                        { "JobPlane", MiningDroneData.FormatPlaneD(jobPlane) },
                        { "JobSite", MiningDroneData.FormatVector3D(jobSite) }
                    }
                });

            } catch (Exception e) {
                var outString = $"Raycast failed: {e.Message}";
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

        public string FormatCoordinates(string coordinateString) {
            var coordinates = coordinateString.Split(' ');
            var x = double.Parse(coordinates[0].Split(':')[1].Trim());
            var y = double.Parse(coordinates[1].Split(':')[1].Trim());
            var z = double.Parse(coordinates[2].Split(':')[1].Trim());
            return $"({x.ToString("0.00")}, {y.ToString("0.00")}, {z.ToString("0.00")})";
        }

        PlaneD ScanJobPlane() {

            Vector3D? p1 = Raycaster.Camera.Raycast(100, 5, 0).HitPosition;
            Vector3D? p2 = Raycaster.Camera.Raycast(100, -5, 5).HitPosition;
            Vector3D? p3 = Raycaster.Camera.Raycast(100, -5, -5).HitPosition;

            if (p1 == null || p2 == null || p3 == null) {
                throw new Exception("Failed to initialize job site plane");
            }

            return new PlaneD(p1.Value, p2.Value, p3.Value);

        }

        Vector3D ScanJobSite() {
            Vector3D jobSite = Raycaster.Camera.Raycast(100, 0, 0).HitPosition.Value;
            if (jobSite == null) {
                throw new Exception("Failed to initialize job site");
            }
            return jobSite;
        }


    }
}
