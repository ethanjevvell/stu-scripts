using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {
            public partial class Maneuvers {
                public class Orientation {

                    public static bool AlignGyro(Vector3D target) {

                        Vector3D targetVectorNormalized = -Vector3D.Normalize(target - CurrentPosition);
                        Vector3D forwardVector = RemoteControl.WorldMatrix.Forward;

                        Vector3D rotationAxis = Vector3D.Cross(forwardVector, targetVectorNormalized);
                        double rotationAngle = Math.Acos(Vector3D.Dot(forwardVector, targetVectorNormalized));

                        if (Math.Abs(rotationAngle - Math.PI) < 0.01) {
                            Array.ForEach(Gyros, gyro => {
                                gyro.SetYaw(0);
                                gyro.SetPitch(0);
                            });
                            return true;
                        }

                        Array.ForEach(Gyros, gyro => {
                            Vector3D localRotationAxis = Vector3D.TransformNormal(rotationAxis, MatrixD.Transpose(RemoteControl.WorldMatrix));
                            gyro.SetYaw((float)localRotationAxis.Y);
                            gyro.SetPitch((float)localRotationAxis.X);
                        });

                        return false;

                    }

                }
            }
        }
    }
}
