using Sandbox.ModAPI.Ingame;
using System;
using VRage.Audio;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class STUOrientationController {

                IMyRemoteControl RemoteControl { get; set; }
                IMyGyro[] Gyros { get; set; }

                private const double ANGLE_ERROR_TOLERANCE = 1e-3;
                private const double DOT_PRODUCT_TOLERANCE = 1e-6;

                public STUOrientationController(IMyRemoteControl remoteControl, IMyGyro[] gyros) {
                    Gyros = gyros;
                    RemoteControl = remoteControl;
                    Array.ForEach(Gyros, gyro => {
                        gyro.GyroOverride = true;
                    });
                }

                public bool AlignShipToTarget(Vector3D target, Vector3D currentPosition) {
                    Vector3D targetVector = target - currentPosition;
                    if (targetVector.LengthSquared() > DOT_PRODUCT_TOLERANCE) {

                        Vector3D targetVectorNormalized = -Vector3D.Normalize(targetVector);
                        Vector3D forwardVector = RemoteControl.WorldMatrix.Forward;

                        Vector3D rotationAxis = Vector3D.Cross(forwardVector, targetVectorNormalized);
                        double dotProduct = MathHelper.Clamp(Vector3D.Dot(forwardVector, targetVectorNormalized), -1, 1);
                        double rotationAngle = Math.Acos(dotProduct);

                        if (Math.Abs(rotationAngle - Math.PI) < ANGLE_ERROR_TOLERANCE) {
                            foreach (var gyro in Gyros) {
                                gyro.Pitch = 0;
                                gyro.Yaw = 0;
                            }
                            CBT.AddToLogQueue($"hit truth condition inside OrientationController.AlignShipToTarget()");
                            return true;
                        }

                        MatrixD worldMatrixTranspose = MatrixD.Transpose(RemoteControl.WorldMatrix);
                        foreach (var gyro in Gyros) {
                            Vector3D localRotationAxis = Vector3D.TransformNormal(rotationAxis, worldMatrixTranspose);
                            gyro.Pitch = (float)localRotationAxis.X;
                            gyro.Yaw = (float)localRotationAxis.Y;
                            CBT.AddToLogQueue($"hit false condition inside OrientationController.AlignShipToTarget()");
                        }
                    }

                    return false;
                }

                public void SetVr(double rollSpeed) {
                    foreach (var gyro in Gyros) {
                        gyro.Roll = (float)rollSpeed;
                    }
                }

                public void SetVp(double pitchSpeed) {
                    foreach (var gyro in Gyros) {
                        gyro.Pitch = (float)pitchSpeed;
                    }
                }

                public void SetVw(double yawSpeed) {
                    foreach (var gyro in Gyros) {
                        gyro.Yaw = (float)yawSpeed;
                    }
                }

            }
        }
    }
}
