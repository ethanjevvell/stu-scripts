using Sandbox.ModAPI.Ingame;
using System;
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
                            return true;
                        }

                        MatrixD worldMatrixTranspose = MatrixD.Transpose(RemoteControl.WorldMatrix);
                        foreach (var gyro in Gyros) {
                            Vector3D localRotationAxis = Vector3D.TransformNormal(rotationAxis, worldMatrixTranspose);
                            gyro.Yaw = (float)localRotationAxis.X;
                            gyro.Pitch = (float)localRotationAxis.Y;
                        }
                    }

                    return false;
                }


                public bool AlignCounterVelocity(Vector3D currentVelocity, Vector3D localCounterVelocity) {

                    currentVelocity.Normalize();
                    localCounterVelocity.Normalize();

                    // Transform local counter velocity to world coordinates
                    Vector3D transformedCounterVelocity = Vector3D.TransformNormal(localCounterVelocity, RemoteControl.WorldMatrix);

                    // Desired direction is opposite to current velocity
                    Vector3D desiredDirection = currentVelocity;

                    // Calculate the angle between the transformed counter velocity and the desired direction
                    double dotProduct = MathHelper.Clamp(Vector3D.Dot(transformedCounterVelocity, desiredDirection), -1, 1);
                    double rotationAngle = Math.Acos(dotProduct);

                    // Check if alignment is within acceptable tolerance
                    if (Math.Abs(Math.PI - rotationAngle) < ANGLE_ERROR_TOLERANCE) {
                        foreach (var gyro in Gyros) {
                            gyro.Yaw = 0;
                            gyro.Pitch = 0;
                            gyro.Roll = 0;
                        }
                        return true;
                    }

                    // Calculate the rotation axis
                    Vector3D rotationAxis = Vector3D.Cross(transformedCounterVelocity, desiredDirection);
                    rotationAxis.Normalize();

                    double error = Math.PI - rotationAngle;
                    Vector3D angularVelocity = rotationAxis * rotationAngle * error;

                    // Transform angular velocity to local coordinates
                    Vector3D localAngularVelocity = Vector3D.TransformNormal(angularVelocity, MatrixD.Transpose(RemoteControl.WorldMatrix));

                    // Correctly map the local angular velocity to gyro controls
                    foreach (var gyro in Gyros) {
                        gyro.Pitch = (float)localAngularVelocity.X;
                        gyro.Yaw = (float)localAngularVelocity.Y;
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
