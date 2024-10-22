using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        STURaycaster Raycaster;
        STUMasterLogBroadcaster JobBroadcaster;
        MyCommandLine CommandLineParser;
        List<LogLCD> LogSubscribers;
        MyIni _ini;

        public Dictionary<string, Action> ProgramCommands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        public Program() {

            _ini = new MyIni();

            LogSubscribers = DiscoverLogSubscribers();
            Raycaster = InitRaycaster();
            Raycaster.RaycastDistance = 10000;
            JobBroadcaster = new STUMasterLogBroadcaster(AUTO_MINER_VARIABLES.AUTO_MINER_HQ_RECON_JOB_LISTENER, IGC, TransmissionDistance.AntennaRelay);

            CommandLineParser = new MyCommandLine();
            ProgramCommands.Add("ScanJobSite", RaycastJobSite);
            ProgramCommands.Add("ToggleRaycast", Raycaster.ToggleRaycast);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;

        }

        public void Main(string argument) {
            try {
                ParseCommand(argument);
            } catch (Exception e) {
                CreateLog($"Error in main loop: {e}", STULogType.ERROR);
            } finally {
                WriteLogs();
            }
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

                CreateLog($"Command {commandString} not recognized.", STULogType.ERROR);
                CreateLog("Available commands:", STULogType.INFO);
                foreach (var command in ProgramCommands.Keys) {
                    CreateLog($"\t{command}", STULogType.INFO);
                }
            }

        }

        void CreateLog(string message, string type) {
            foreach (var lcd in LogSubscribers) {
                lcd.FlightLogs.Enqueue(
                   new STULog() {
                       Message = message,
                       Sender = AUTO_MINER_VARIABLES.AUTO_MINER_RECON_NAME,
                       Type = type
                   }
                   );
            }
        }

        void WriteLogs() {
            foreach (var lcd in LogSubscribers) {
                lcd.Update();
            }
        }

        public void RaycastJobSite() {
            try {

                PlaneD jobPlane = ScanJobPlane();
                Vector3D jobSite = ScanJobSite();

                int jobDepth;
                if (!int.TryParse(CommandLineParser.Argument(1), out jobDepth)) {
                    throw new Exception("Invalid job depth argument; command format is 'ScanJobSite 70', where '70' denotes depth in meters");
                }

                JobBroadcaster.Log(new STULog {
                    Sender = AUTO_MINER_VARIABLES.AUTO_MINER_RECON_NAME,
                    Message = "Transmitting new job site",
                    Type = STULogType.INFO,
                    Metadata = new Dictionary<string, string> {
                        { "JobPlane", MiningDroneData.FormatPlaneD(jobPlane) },
                        { "JobSite", MiningDroneData.FormatVector3D(jobSite) },
                        { "JobDepth", jobDepth.ToString() },
                    }
                });

                CreateLog("Sent job to HQ", STULogType.OK);

            } catch (Exception e) {
                var outString = $"Raycast failed: {e.Message}";
                CreateLog(outString, STULogType.ERROR);
            }
        }

        List<LogLCD> DiscoverLogSubscribers() {
            List<IMyTextPanel> logPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(
                logPanels,
                block => MyIni.HasSection(block.CustomData, AUTO_MINER_VARIABLES.AUTO_MINER_RECON_LOG_SUBSCRIBER_TAG)
                && block.CubeGrid == Me.CubeGrid);
            return new List<LogLCD>(logPanels.ConvertAll(panel => {
                MyIniParseResult result;
                if (!_ini.TryParse(panel.CustomData, out result)) {
                    throw new Exception($"Error parsing log configuration: {result}");
                }
                int displayIndex = _ini.Get(AUTO_MINER_VARIABLES.AUTO_MINER_RECON_LOG_SUBSCRIBER_TAG, "DisplayIndex").ToInt32(0);
                double fontSize = _ini.Get(AUTO_MINER_VARIABLES.AUTO_MINER_RECON_LOG_SUBSCRIBER_TAG, "FontSize").ToDouble(1.0);
                string font = _ini.Get(AUTO_MINER_VARIABLES.AUTO_MINER_RECON_LOG_SUBSCRIBER_TAG, "Font").ToString("Monospace");
                return new LogLCD(panel, displayIndex, font, (float)fontSize);
            }));
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
