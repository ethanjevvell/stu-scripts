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
            private IEnumerator<bool> imageStateMachine;
            private bool finishedTakingImage;
            private STUImage image;

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
                set { raycastPitch = MathHelper.Clamp(value, -45, 45); }
            }
            public float RaycastYaw {
                get { return raycastYaw; }
                set { raycastYaw = MathHelper.Clamp(value, -45, 45); }
            }
            public bool FinishedTakingImage {
                get { return finishedTakingImage; }
                private set { finishedTakingImage = value; }
            }
            public STUImage Image {
                get { return image; }
                private set { image = value; }
            }
            public IEnumerator<bool> ImageStateMachine {
                get { return imageStateMachine; }
                private set { imageStateMachine = value; }
            }
            #endregion

            public STURaycaster(IMyCameraBlock camera) {
                Camera = camera;
                Camera.EnableRaycast = true;
                FinishedTakingImage = false;
                Image = new STUImage();
            }

            /// <summary>
            /// Fires a raycast from the camera and returns the hit entity info. Uses RaycastDistance, RaycastPitch, and RaycastYaw.
            /// </summary>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public MyDetectedEntityInfo Raycast() {
                if (!Camera.CanScan(RaycastDistance)) {
                    return default(MyDetectedEntityInfo);
                }
                return Camera.Raycast(RaycastDistance, RaycastPitch, RaycastYaw);
            }

            public void ToggleRaycast(bool on) {
                Camera.EnableRaycast = on;
            }

            public void ToggleRaycast() {
                Camera.EnableRaycast = !Camera.EnableRaycast;
            }

            /// <summary>
            /// Runs a state machine to take an image over time. The image is stored in the Image property.
            /// Poll the FinishedTakingImage property to check if the image is done before attempting to draw it with STUDisplay.DrawCustomImageOverTime()
            /// The raycasting camera MUST be stationary, unless the resolution and distance are very small and can be executed in a single tick
            /// </summary>
            /// <param name="distance"></param>
            /// <param name="fov"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public void TakeImageOverTime(float distance, float fov, uint x, uint y, Action<string> echo) {
                if (ImageStateMachine != null && !FinishedTakingImage) {
                    bool hasMoreSteps = ImageStateMachine.MoveNext();
                    if (!hasMoreSteps) {
                        ImageStateMachine.Dispose();
                        ImageStateMachine = null;
                        FinishedTakingImage = true;
                        // Disable raycasting to save performance and battery
                        Camera.EnableRaycast = false;
                    }
                } else {
                    ImageStateMachine = RunImageCoroutine(distance, fov, x, y, echo);
                    Camera.EnableRaycast = true;
                }
            }

            /// <summary>
            /// Coroutine for taking an image over time. The image is stored in the Image property.
            /// Uses perspective projection to mitigate the fisheye effect.
            /// Not to be called directly.
            /// </summary>
            /// <param name="distance"></param>
            /// <param name="fov"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            IEnumerator<bool> RunImageCoroutine(float distance, float fov, uint x, uint y, Action<string> echo) {
                // Ensure the camera is enabled
                Camera.Enabled = true;
                yield return false;

                // Convert field of view from degrees to radians
                float fov_rad = MathHelper.ToRadians(fov);

                // Calculate aspect ratio
                float aspectRatio = (float)x / y;

                // Compute the vertical field of view based on the horizontal FOV and aspect ratio
                float fov_y_rad = 2 * (float)Math.Atan(Math.Tan(fov_rad / 2) / aspectRatio);

                // Precompute tangent values for efficiency
                float tan_fov_x = (float)Math.Tan(fov_rad / 2);
                float tan_fov_y = (float)Math.Tan(fov_y_rad / 2);

                for (int r = 0; r < y; r++) {
                    List<STUImage.Pixel> row = new List<STUImage.Pixel>();
                    for (int c = 0; c < x; c++) {
                        // Wait until the camera can scan the distance
                        while (!Camera.CanScan(distance)) {
                            yield return false;
                        }

                        // Compute normalized device coordinates (NDC) in the range [-1, 1]
                        float ndc_x = ((c + 0.5f) / x) * 2f - 1f; // Horizontal coordinate
                        float ndc_y = 1f - ((r + 0.5f) / y) * 2f; // Vertical coordinate (flipped for image coordinates)

                        Vector3 dir = new Vector3(
                            ndc_x * tan_fov_x,
                            ndc_y * tan_fov_y,
                            1f // Assuming the camera looks along the positive Z-axis
                        );

                        // Normalize the direction vector
                        dir.Normalize();

                        // Compute yaw and pitch angles from the direction vector
                        float yaw_rad = (float)Math.Atan2(dir.X, dir.Z);
                        float pitch_rad = (float)Math.Asin(dir.Y);

                        // Convert yaw and pitch from radians to degrees
                        float currentYaw = MathHelper.ToDegrees(yaw_rad);
                        float currentPitch = MathHelper.ToDegrees(pitch_rad);

                        // Perform the raycast with the computed yaw and pitch angles
                        MyDetectedEntityInfo hit = Camera.Raycast(distance, currentPitch, currentYaw);

                        STUImage.Pixel pixel = new STUImage.Pixel {
                            distanceVal = hit.IsEmpty() ? -1 : (float)Vector3D.Distance(hit.HitPosition.Value, Camera.GetPosition())
                        };

                        row.Add(pixel);

                    }
                    Image.PixelArray.Add(row);
                }
                Camera.Enabled = false;
            }
            /// <summary>
            /// Returns a string with the hit info. Always check if a hit.IsEmpty() before using this.
            /// </summary>
            /// <param name="hit"></param>
            public string GetHitInfoString(MyDetectedEntityInfo hit) {
                double distance = Vector3D.Distance((Vector3D)hit.HitPosition, Camera.GetPosition());
                return $"Name: {hit.Name}\n" +
                    $"Type: {hit.Type}\n" +
                    $"Position: {hit.Position}\n" +
                    $"Velocity: {hit.Velocity}\n" +
                    $"Relationship: {hit.Relationship}\n" +
                    $"Distance: {distance}\n" +
                    $"Remaining Range: {Camera.AvailableScanRange}\n";
                ;
            }

            public static Dictionary<string, string> GetHitInfoDictionary(MyDetectedEntityInfo hitInfo) {
                var hitPos = hitInfo.HitPosition ?? Vector3D.Zero;
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

            /// <summary>
            /// Turns a hit info dictionary<string, string> into a MyDetectedEntityInfo. Always surround with try/catch.
            /// </summary>
            /// <param name="hitInfoDictionary"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public static MyDetectedEntityInfo DeserializeHitInfo(Dictionary<string, string> hitInfoDictionary) {

                Vector3 velocity;

                try {
                    velocity = DeserializeVector3(hitInfoDictionary["Velocity"]);
                } catch {
                    throw new Exception(hitInfoDictionary["Velocity"]);
                }

                Vector3D hitPosition;
                bool parsedPosition = Vector3D.TryParse(hitInfoDictionary["Position"], out hitPosition);

                if (!parsedPosition) {
                    throw new Exception($"ERR: V = {hitInfoDictionary["Velocity"]}, P = {hitInfoDictionary["Position"]}");
                }

                long entityId = long.Parse(hitInfoDictionary["EntityId"]);
                string name = hitInfoDictionary["Name"];
                MyDetectedEntityType type = (MyDetectedEntityType)Enum.Parse(typeof(MyDetectedEntityType), hitInfoDictionary["Type"]);
                MatrixD orientation = DeserializeMatrixD(hitInfoDictionary["Orientation"]);
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

            private static Vector3 DeserializeVector3(string vectorString) {
                vectorString = vectorString.Replace("{", "");
                vectorString = vectorString.Replace("}", "");
                string[] vectorStringParts = vectorString.Split(' ');
                double x = double.Parse(vectorStringParts[0].Split(':')[1]);
                double y = double.Parse(vectorStringParts[1].Split(':')[1]);
                double z = double.Parse(vectorStringParts[2].Split(':')[1]);
                return new Vector3(x, y, z);
            }
        }
    }
}
