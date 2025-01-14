
using System;
using System.Text;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public static class AUTO_MINER_VARIABLES {

            public const string AUTO_MINER_RECON_NAME = "AM-R";
            public const string AUTO_MINER_RECON_LOG_SUBSCRIBER_TAG = "AUTO_MINER_RECON_LOG_SUBSCRIBER";

            public const string AUTO_MINER_HQ_RECON_JOB_LISTENER = "AUTO_MINER_HQ_RECON_JOB_LISTENER";

            public const string AUTO_MINER_HQ_DRONE_TELEMETRY_CHANNEL = "AUTO_MINER_HQ_DRONE_TELEMETRY_CHANNEL";
            public const string AUTO_MINER_HQ_DRONE_LOG_CHANNEL = "AUTO_MINER_HQ_DRONE_LOG_CHANNEL";

            public const string AUTO_MINER_DRONE_COMMAND_CHANNEL = "AUTO_MINER_DRONE_COMMAND_CHANNEL";

            public const string AUTO_MINER_HQ_COMMAND_CHANNEL = "AUTO_MINER_HQ_COMMAND_CHANNEL";
            public const string AUTO_MINER_HQ_NAME = "AM-HQ";

            public const string AUTO_MINER_LOG_SUBSCRIBER_TAG = "AUTO_MINER_LOG_SUBSCRIBER";
            public const string AUTO_MINER_HQ_MAIN_SUBSCRIBER_TAG = "AUTO_MINER_MAIN_SUBSCRIBER";

        }

        public class MiningDroneData {

            public string Id { get; set; }
            public string Name { get; set; }
            public string State { get; set; }
            public Vector3D WorldPosition { get; set; }
            public Vector3D WorldVelocity { get; set; }
            public Vector3D WorldAcceleration { get; set; }
            public double HydrogenLiters { get; set; }
            public double HydrogenCapacity { get; set; }
            public double PowerMegawatts { get; set; }
            public double PowerCapacity { get; set; }
            public double CargoLevel { get; set; }
            public double CargoCapacity { get; set; }
            public PlaneD JobPlane { get; set; }
            public Vector3D JobSite { get; set; }
            public int JobDepth { get; set; }

            public static MiningDroneData Deserialize(string s) {
                var drone = new MiningDroneData();

                var parts = s.Split(';');
                foreach (var part in parts) {
                    var kv = part.Split(':');
                    if (kv.Length != 2)
                        continue;

                    var key = kv[0].Trim();
                    var value = kv[1].Trim();

                    switch (key) {
                        case "Id":
                            drone.Id = value;
                            break;
                        case "Name":
                            drone.Name = value;
                            break;
                        case "State":
                            drone.State = value;
                            break;
                        case "WorldPosition":
                            drone.WorldPosition = DeserializeVector3D(value);
                            break;
                        case "WorldVelocity":
                            drone.WorldVelocity = DeserializeVector3D(value);
                            break;
                        case "WorldAcceleration":
                            drone.WorldAcceleration = DeserializeVector3D(value);
                            break;
                        case "HydrogenLiters":
                            drone.HydrogenLiters = double.Parse(value);
                            break;
                        case "HydrogenCapacity":
                            drone.HydrogenCapacity = double.Parse(value);
                            break;
                        case "PowerMegawatts":
                            drone.PowerMegawatts = double.Parse(value);
                            break;
                        case "PowerCapacity":
                            drone.PowerCapacity = double.Parse(value);
                            break;
                        case "CargoLevel":
                            drone.CargoLevel = double.Parse(value);
                            break;
                        case "CargoCapacity":
                            drone.CargoCapacity = double.Parse(value);
                            break;
                        case "JobPlane":
                            drone.JobPlane = DeserializePlaneD(value);
                            break;
                        case "JobSite":
                            drone.JobSite = DeserializeVector3D(value);
                            break;
                        case "JobDepth":
                            drone.JobDepth = int.Parse(value);
                            break;
                    }
                }

                return drone;
            }

            public static string FormatVector3D(Vector3D v) {
                return $"({v.X}, {v.Y}, {v.Z})";
            }

            public static Vector3D DeserializeVector3D(string s) {
                // Remove parentheses and split by comma
                s = s.Trim('(', ')');
                var parts = s.Split(',');
                if (parts.Length != 3)
                    throw new FormatException($"Invalid Vector3D format: {s}");

                var x = double.Parse(parts[0]);
                var y = double.Parse(parts[1]);
                var z = double.Parse(parts[2]);

                return new Vector3D(x, y, z);
            }

            public static PlaneD DeserializePlaneD(string s) {
                // Remove parentheses and split by comma
                s = s.Trim('(', ')');
                var parts = s.Split(',');
                if (parts.Length != 4)
                    throw new FormatException("Invalid PlaneD format");

                var x = double.Parse(parts[0]);
                var y = double.Parse(parts[1]);
                var z = double.Parse(parts[2]);
                var d = double.Parse(parts[3]);

                return new PlaneD(x, y, z, d);
            }

            public string Serialize() {
                var sb = new StringBuilder();

                sb.Append($"Id: {Id}; ");
                sb.Append($"Name: {Name}; ");
                sb.Append($"State: {State}; ");
                sb.Append($"WorldPosition: {FormatVector3D(WorldPosition)}; ");
                sb.Append($"WorldVelocity: {FormatVector3D(WorldVelocity)}; ");
                sb.Append($"WorldAcceleration: {FormatVector3D(WorldAcceleration)}; ");
                sb.Append($"HydrogenLiters: {HydrogenLiters}; ");
                sb.Append($"PowerMegawatts: {PowerMegawatts}; ");
                sb.Append($"HydrogenCapacity: {HydrogenCapacity}; ");
                sb.Append($"PowerCapacity: {PowerCapacity}; ");
                sb.Append($"CargoLevel: {CargoLevel}; ");
                sb.Append($"JobSite: {FormatVector3D(JobSite)}; ");
                sb.Append($"JobPlane: {FormatPlaneD(JobPlane)}; ");
                sb.Append($"JobDepth: {JobDepth}; ");
                sb.Append($"CargoCapacity: {CargoCapacity}");

                return sb.ToString();
            }

            public static string FormatPlaneD(PlaneD p) {
                return $"({p.Normal.X}, {p.Normal.Y}, {p.Normal.Z}, {p.D})";
            }

        }

        public class MinerState {
            public const string INITIALIZE = "INITIALIZE";
            public const string IDLE = "IDLE";
            public const string FLY_TO_JOB_SITE = "FLY_TO_JOB_SITE";
            public const string FLY_TO_HOME_BASE = "FLY_TO_HOME_BASE";
            public const string REFUELING = "REFUELING";
            public const string MINING = "MINING";
            public const string RTB = "RTB";
            public const string HARD_FAILURE = "HARD_FAILURE";
            public const string MISSING = "MISSING";
            public const string ALIGN_WITH_BASE_CONNECTOR = "ALIGN_WITH_BASE_CONNECTOR";
            public const string DOCKING = "DOCKING";
            public const string DEPOSIT_ORES = "DEPOSIT_ORES";
            public const string DEPOSIT_IDLE = "DEPOSIT_IDLE";
            public const string ERROR = "ERROR";
            public const string EMERGENCY_LANDING = "EMERGENCY_LANDING";
        }

    }
}
