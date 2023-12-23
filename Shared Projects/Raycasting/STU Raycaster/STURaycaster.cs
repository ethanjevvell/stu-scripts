using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public class STURaycaster {

            private IMyCameraBlock camera;
            private float raycastDistance = 10000;
            private float raycastPitch = 0;
            private float raycastYaw = 0;

            // Getters and Setters
            #region
            public IMyCameraBlock Camera {
                get { return camera; }
                set { camera = value; }
            }

            public float RaycastDistance {
                get { return raycastDistance; }
                set { raycastDistance = value; }
            }

            public float RaycastPitch {
                get { return raycastPitch; }
                set { raycastPitch = value; }
            }

            public float RaycastYaw {
                get { return raycastYaw; }
                set { raycastYaw = value; }
            }
            #endregion

            public STURaycaster(IMyCameraBlock camera) {
                Camera = camera;
                Camera.EnableRaycast = true;
            }

            public MyDetectedEntityInfo Raycast(double distance = double.NaN) {
                distance = double.IsNaN(distance) ? RaycastDistance : distance;
                if (!Camera.CanScan(distance)) {
                    throw new Exception();
                }
                return Camera.Raycast(distance, RaycastPitch, RaycastYaw);
            }

            public void ToggleRaycast() {
                Camera.EnableRaycast = !Camera.EnableRaycast;
            }

            public string GetHitInfoString(MyDetectedEntityInfo hit) {
                var hitPos = hit.HitPosition ?? Vector3D.Zero;
                var distance = hitPos == Vector3.Zero ? 0 : Vector3D.Distance(hitPos, Camera.GetPosition());
                return $"Name: {hit.Name}\n" +
                    $"Type: {hit.Type}\n" +
                    $"Position: {hit.Position}\n" +
                    $"Velocity: {hit.Velocity}\n" +
                    $"Relationship: {hit.Relationship}\n" +
                    $"Distance: {distance}\n" +
                    $"Remaining Range: {Camera.AvailableScanRange}\n";
                ;
            }

            public Dictionary<string, string> GetHitInfoDictionary(MyDetectedEntityInfo hitInfo) {
                var hitPos = hitInfo.HitPosition ?? Vector3D.Zero;
                var distance = hitPos == Vector3.Zero ? 0 : Vector3D.Distance(hitPos, Camera.GetPosition());
                return new Dictionary<string, string>
                {
                    { "Name", hitInfo.Name },
                    { "Type", hitInfo.Type.ToString() },
                    { "Position", hitInfo.Position.ToString() },
                    { "Velocity", hitInfo.Velocity.ToString() },
                    { "Size", hitInfo.BoundingBox.Size.ToString() },
                    { "Orientation", SerializeMatrixD(hitInfo.Orientation) },
                    { "HitPosition", hitInfo.HitPosition.ToString() },
                    { "TimeStamp", hitInfo.TimeStamp.ToString() },
                    { "Relationship", hitInfo.Relationship.ToString() },
                    { "BoundingBox", SerializeBoundingBoxD(hitInfo.BoundingBox)},
                    { "EntityId", hitInfo.EntityId.ToString() },
                };
            }

            public static MyDetectedEntityInfo DeserializeHitInfo(Dictionary<string, string> hitInfoDictionary) {
                Vector3D hitPosition;
                Vector3D velocity;

                long entityId = long.Parse(hitInfoDictionary["EntityId"]);
                string name = hitInfoDictionary["Name"];
                MyDetectedEntityType type = (MyDetectedEntityType)Enum.Parse(typeof(MyDetectedEntityType), hitInfoDictionary["Type"]);
                Vector3D.TryParse(hitInfoDictionary["HitPosition"], out hitPosition);
                MatrixD orientation = DeserializeMatrixD(hitInfoDictionary["Orientation"]);
                Vector3D.TryParse(hitInfoDictionary["Velocity"], out velocity);
                BoundingBoxD boundingBox = DeserializeBoundingBoxD(hitInfoDictionary["BoundingBox"]);
                MyRelationsBetweenPlayerAndBlock relationship = (MyRelationsBetweenPlayerAndBlock)Enum.Parse(typeof(MyRelationsBetweenPlayerAndBlock), hitInfoDictionary["Relationship"]);
                long timeStamp = long.Parse(hitInfoDictionary["TimeStamp"]);

                return new MyDetectedEntityInfo(entityId, name, type, hitPosition, orientation, velocity, relationship, boundingBox, timeStamp);
            }

            private static string SerializeBoundingBoxD(BoundingBoxD boundingBox) {
                return $"{boundingBox.Min.X},{boundingBox.Min.Y},{boundingBox.Min.Z}:{boundingBox.Max.X},{boundingBox.Max.Y},{boundingBox.Max.Z}";
            }

            private static BoundingBoxD DeserializeBoundingBoxD(string data) {
                var parts = data.Split(':');
                var minParts = parts[0].Split(',');
                var maxParts = parts[1].Split(',');

                Vector3D min = new Vector3D(
                    Convert.ToDouble(minParts[0]),
                    Convert.ToDouble(minParts[1]),
                    Convert.ToDouble(minParts[2])
                );

                Vector3D max = new Vector3D(
                    Convert.ToDouble(maxParts[0]),
                    Convert.ToDouble(maxParts[1]),
                    Convert.ToDouble(maxParts[2])
                );

                return new BoundingBoxD(min, max);
            }

            private static string SerializeMatrixD(MatrixD matrix) {
                return string.Join(",",
                    matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                    matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                    matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                    matrix.M41, matrix.M42, matrix.M43, matrix.M44);
            }

            private static MatrixD DeserializeMatrixD(string data) {
                var elements = data.Split(',').Select(double.Parse).ToArray();

                return new MatrixD(
                    elements[0], elements[1], elements[2], elements[3],
                    elements[4], elements[5], elements[6], elements[7],
                    elements[8], elements[9], elements[10], elements[11],
                    elements[12], elements[13], elements[14], elements[15]);
            }
        }
    }
}
