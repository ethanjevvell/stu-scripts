﻿using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public class STUOrientationController {

                IMyRemoteControl RemoteControl { get; set; }
                IMyGyro[] Gyros { get; set; }

                const double ANGLE_ERROR_TOLERANCE = 1e-2;
                const double DOT_PRODUCT_TOLERANCE = 1e-6;
                const double ANGULAR_VELOCITY_GAIN = 1.8;

                public STUOrientationController(IMyRemoteControl remoteControl, IMyGyro[] gyros) {
                    Gyros = gyros;
                    RemoteControl = remoteControl;
                    Array.ForEach(Gyros, gyro => {
                        gyro.GyroOverride = true;
                    });
                }

                /// <summary>
                /// Aligns the ship's forward vector to the target vector
                /// </summary>
                /// <param name="target"></param>
                /// <param name="currentPosition"></param>
                /// <param name="referenceBlock"></param>
                /// <returns></returns>
                public bool AlignShipToTarget(Vector3D target, Vector3D currentPosition, IMyTerminalBlock referenceBlock = null) {

                    // If we don't pass in a reference block, use the remote control
                    if (referenceBlock == null) {
                        CBT.AddToLogQueue("No reference block provided, using remote control");
                        referenceBlock = RemoteControl;
                    }

                    // Adjust the target vector to account for the spatial offset of the reference block. 
                    Vector3D targetVector = Vector3D.Normalize(target - referenceBlock.GetPosition());

                    // Default to the forward vector of the reference block. Fix later?
                    Vector3D forwardVector = Vector3D.Normalize(referenceBlock.WorldMatrix.Forward);

                    CBT.AddToLogQueue($"forwardVector: {forwardVector}");

                    double dotProduct = MathHelper.Clamp(Vector3D.Dot(forwardVector, targetVector), -1, 1);
                    double rotationAngle = Math.Acos(dotProduct);

                    if (Math.Abs(rotationAngle) < ANGLE_ERROR_TOLERANCE) {
                        HardStopGyros();
                        return true;
                    }

                    Vector3D rotationAxis = Vector3D.Cross(forwardVector, targetVector);
                    rotationAxis.Normalize();

                    double proportionalError = rotationAngle * -ANGULAR_VELOCITY_GAIN;

                    Vector3D angularVelocity = rotationAxis * proportionalError;

                    ApplyGyroTransformedAngularVelocity(angularVelocity);

                    return false;
                }

                /// <summary>
                /// Aligns the ship's forward vector to the target vector
                /// </summary>
                /// <param name="target"></param>
                /// <param name="currentPosition"></param>
                /// <param name="referenceVector"></param>
                /// <returns></returns>
                public bool AlignShipToTarget(Vector3D target, Vector3D currentPosition, Vector3D referenceVector) {

                    Vector3D targetVector = Vector3D.Normalize(target - currentPosition);
                    Vector3D forwardVector = Vector3D.Normalize(referenceVector);

                    double dotProduct = MathHelper.Clamp(Vector3D.Dot(forwardVector, targetVector), -1, 1);
                    double rotationAngle = Math.Acos(dotProduct);

                    if (Math.Abs(rotationAngle) < ANGLE_ERROR_TOLERANCE) {
                        HardStopGyros();
                        return true;
                    }

                    Vector3D rotationAxis = Vector3D.Cross(forwardVector, targetVector);
                    rotationAxis.Normalize();

                    double proportionalError = rotationAngle * -ANGULAR_VELOCITY_GAIN;

                    Vector3D angularVelocity = rotationAxis * proportionalError;

                    // Map the local angular velocity to gyro controls
                    foreach (var gyro in Gyros) {
                        Vector3D localAngularVelocity = STUTransformationUtils.WorldDirectionToLocalDirection(gyro, angularVelocity);
                        gyro.Pitch = (float)localAngularVelocity.X;
                        gyro.Yaw = (float)localAngularVelocity.Y;
                        // gyro.Roll = (float)localAngularVelocity.Z;
                    }
                    ApplyGyroTransformedAngularVelocity(angularVelocity);

                    return false;
                }

                public bool AlignCounterVelocity(Vector3D currentVelocity, Vector3D localCounterVelocity) {

                    currentVelocity.Normalize();
                    localCounterVelocity.Normalize();

                    // Transform local counter velocity to world coordinates
                    Vector3D transformedCounterVelocity = STUTransformationUtils.LocalDirectionToWorldDirection(RemoteControl, localCounterVelocity);

                    // Desired direction is opposite to current velocity
                    Vector3D desiredDirection = currentVelocity;

                    // Calculate the angle between the transformed counter velocity and the desired direction
                    double dotProduct = MathHelper.Clamp(Vector3D.Dot(transformedCounterVelocity, desiredDirection), -1, 1);
                    double rotationAngle = Math.Acos(dotProduct);

                    // Check if alignment is within acceptable tolerance
                    if (Math.Abs(Math.PI - rotationAngle) < ANGLE_ERROR_TOLERANCE) {
                        HardStopGyros();
                        return true;
                    }

                    // Calculate the rotation axis
                    Vector3D rotationAxis = Vector3D.Cross(transformedCounterVelocity, desiredDirection);
                    rotationAxis.Normalize();

                    double error = Math.PI - rotationAngle;
                    Vector3D angularVelocity = rotationAxis * rotationAngle * error;

                    ApplyGyroTransformedAngularVelocity(angularVelocity);

                    return false;
                }

                public void ApplyGyroTransformedAngularVelocity(Vector3D angularVelocity) {
                    foreach (var gyro in Gyros) {
                        Vector3D localAngularVelocity = STUTransformationUtils.WorldDirectionToLocalDirection(gyro, angularVelocity);
                        gyro.Pitch = (float)localAngularVelocity.X;
                        gyro.Yaw = (float)localAngularVelocity.Y;
                        gyro.Roll = (float)localAngularVelocity.Z;
                    }
                }

                public void HardStopGyros() {
                    foreach (var gyro in Gyros) {
                        gyro.Pitch = 0;
                        gyro.Yaw = 0;
                        gyro.Roll = 0;
                    }
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
