using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class STUOrientationController {

                IMyRemoteControl RemoteControl { get; set; }
                IMyGyro[] Gyros { get; set; }

                public STUOrientationController(IMyRemoteControl remoteControl, IMyGyro[] gyros) {
                    Gyros = gyros;
                    RemoteControl = remoteControl;
                    Array.ForEach(Gyros, gyro => {
                        gyro.GyroOverride = true;
                    });
                }

                public bool AlignShipToTarget(Vector3D target, Vector3D currentPosition) {

                    Vector3D targetVectorNormalized = -Vector3D.Normalize(target - currentPosition);
                    Vector3D forwardVector = RemoteControl.WorldMatrix.Forward;

                    Vector3D rotationAxis = Vector3D.Cross(forwardVector, targetVectorNormalized);
                    double rotationAngle = Math.Acos(Vector3D.Dot(forwardVector, targetVectorNormalized));

                    if (Math.Abs(rotationAngle - Math.PI) < 0.01) {
                        Array.ForEach(Gyros, gyro => {
                            gyro.Pitch = 0;
                            gyro.Yaw = 0;
                        });
                        return true;
                    }

                    Array.ForEach(Gyros, gyro => {
                        Vector3D localRotationAxis = Vector3D.TransformNormal(rotationAxis, MatrixD.Transpose(RemoteControl.WorldMatrix));
                        gyro.Pitch = (float)localRotationAxis.X;
                        gyro.Yaw = (float)localRotationAxis.Y;
                    });

                    return false;

                }

            }
        }
    }
}
