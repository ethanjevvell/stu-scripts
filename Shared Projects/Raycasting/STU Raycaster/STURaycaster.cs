using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public class STURaycaster {

            IMyCameraBlock Camera { get; set; }
            public double RaycastDistance = 2000;

            public STURaycaster(IMyCameraBlock camera) {
                Camera = camera;
                Camera.EnableRaycast = true;
            }

            public MyDetectedEntityInfo Raycast(double distance = double.NaN) {

                if (double.IsNaN(distance)) {
                    distance = RaycastDistance;
                }

                if (Camera.CanScan(distance) == false) {
                    throw new Exception();
                }

                return Camera.Raycast(distance);
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
                    $"Distance: {distance}\n";

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
                    { "Orientation", hitInfo.Orientation.ToString() },
                    { "HitPosition", hitInfo.HitPosition.ToString() },
                    { "TimeStamp", hitInfo.TimeStamp.ToString() },
                    { "Relationship", hitInfo.Relationship.ToString() },
                    { "BoundingBoxng", hitInfo.BoundingBox.ToString() },
                    { "EntityId", hitInfo.EntityId.ToString() },
                    { "Distance", distance.ToString() }
                };
            }

        }
    }
}
