using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        bool halt = false;

        public IMyGyro Gyro;

        public Vector3D TestOne = new Vector3D(581.42, -344.59, -897.14);
        public Vector3D TestTwo = new Vector3D(593.26, -370.04, -912.82);
        public Vector3D TestThree = new Vector3D(622.44, -331.39, -910.35);

        Vector3D gyroForwardVec = new Vector3D();
        Vector3D gyroUpVec = new Vector3D();
        Vector3D gyroRightVec = new Vector3D();

        Vector3D gyroForwardVecAbs = new Vector3D();
        Vector3D gyroUpVecAbs = new Vector3D();
        Vector3D gyroRightVecAbs = new Vector3D();

        public Program() {

            Gyro = GridTerminalSystem.GetBlockWithName("Gyroscope") as IMyGyro;

        }

        public void Save() {

        }

        public void Main(string argument) {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            Vector3D gyroPosition = Gyro.GetPosition();
            Vector3D targetPosition = TestOne;
            Vector3D directionVectorAbsolute = targetPosition - gyroPosition;
            Vector3D directionVectorNormalized = Vector3D.Normalize(targetPosition - gyroPosition);

            // get gyro's forward, up, and right vectors
            gyroForwardVec = Base6DirectionToVector3(Gyro.Orientation.Forward);
            gyroUpVec = Base6DirectionToVector3(Gyro.Orientation.Up);
            gyroRightVec = CalculateRightVector();
            Echo($"gyroForwardVec: {gyroForwardVec}\n" +
                $"gyroUpVec: {gyroUpVec}\n" +
                $"gyroRightVec: {gyroRightVec}\n");
            // basically just a lookup table. see Base6DirectionToVector3. values do not update on code iterations.

            // get the absolute orientation of each vector. here "absolute" means w.r.t. the world origin.
            gyroForwardVecAbs = Vector3D.TransformNormal(gyroForwardVec, Gyro.WorldMatrix);
            gyroUpVecAbs = Vector3D.TransformNormal(gyroUpVec, Gyro.WorldMatrix);
            gyroRightVecAbs = Vector3D.TransformNormal(gyroRightVec, Gyro.WorldMatrix);

            // messing around with the Base6Directions class
            Base6Directions.Direction gyroForwardDir = Base6Directions.GetDirection(gyroForwardVec);
            Base6Directions.Direction gyroUpDir = Base6Directions.GetDirection(gyroUpVec);
            Base6Directions.Direction gyroRightDir = Base6Directions.GetDirection(gyroRightVec);

            Echo($"gyroForwardDir: {gyroForwardDir}\n" +
                $"gyroUpDir: {gyroUpDir}\n" +
                $"gyroRightDir: {gyroRightDir}\n");

            double yawAngle = CalculateYawAngle(gyroForwardVecAbs, gyroUpVec, directionVectorNormalized);
            double pitchAngle = CalculatePitchAngle(gyroForwardVecAbs, gyroRightVec, directionVectorNormalized);
            Echo($"yawAngle = {yawAngle}\n" +
                $"pitchAngle = {pitchAngle}\n");

            Pitch(Gyro, pitchAngle);
            Yaw(Gyro, yawAngle);

            if (argument.Contains("reset")) {
                Reset(Gyro);
            }

            if (halt) {
                Echo("Program halting...");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                Reset(Gyro);
            }
        }

        public double CalculateYawAngle(Vector3D gyroForward, Vector3D gyroUp, Vector3D directionVector) {
            // project gyro's directional vectors onto the horizontal plane by removing the up component
            Vector3D forwardHorizontal = Vector3D.Reject(gyroForward, gyroUp);
            Vector3D targetHorizontal = Vector3D.Reject(directionVector, gyroUp);

            // calculate the angle
            double angle = Math.Acos(Vector3D.Dot(forwardHorizontal, targetHorizontal) / (forwardHorizontal.Length() * targetHorizontal.Length()));

            // determine direction of rotation using cross product
            if (Vector3D.Cross(forwardHorizontal, targetHorizontal).Dot(gyroUp) < 0) {
                angle = -angle;
            }

            return angle;
        }

        public double CalculatePitchAngle(Vector3D gyroForward, Vector3D gyroRight, Vector3D directionVector) {
            // project the gyro's forward direction and the target drection onto a plane defined by
            // the gyro's right vector, i.e., the plane perpendicular to the right vector (the plane on which 
            // we rotate to achieve pitch
            Vector3D forwardOnPlane = Vector3D.Reject(gyroForward, gyroRight);
            Vector3D targetOnPlane = Vector3D.Reject(directionVector, gyroRight);

            // Normalize vectors to only consider directions, i.e. disregard distance
            // Normalize() essentially makes the magnitude 1 (? - this was my own thought, not GPT)
            forwardOnPlane.Normalize();
            targetOnPlane.Normalize();

            // calculate the angle between the two vectors
            double angle = Math.Acos(Vector3D.Dot(forwardOnPlane, targetOnPlane));

            // determine the direction of the pitch using cross product
            // If the cross product in the direction of gyroRight is negative, invert the angle
            if (Vector3D.Cross(forwardOnPlane, targetOnPlane).Dot(gyroRight) < 0) {
                angle = -angle;
            }

            return angle;
        }

        public void Reset(IMyGyro gyro) {
            Echo("resetting gyro moments to 0");
            gyro.GyroOverride = true;
            gyro.Pitch = 0;
            gyro.Roll = 0;
            gyro.Yaw = 0;
            halt = true;
        }

        public void Pitch(IMyGyro gyro, double pitchSpeed) {
            Echo($"pitching at a speed of {pitchSpeed}");
            gyro.Pitch = (float)pitchSpeed;
        }

        public void Roll(IMyGyro gyro, double rollSpeed) {
            Echo($"rolling at a speed of {rollSpeed}");
            gyro.Roll = (float)rollSpeed;
        }

        public void Yaw(IMyGyro gyro, double yawSpeed) {
            Echo($"yawing at a speed of {yawSpeed}");
            gyro.Yaw = (float)yawSpeed;
        }

        public Vector3 Base6DirectionToVector3(Base6Directions.Direction direction) {
            switch (direction) {
                case Base6Directions.Direction.Forward:
                    return new Vector3(0, 0, 1);
                case Base6Directions.Direction.Backward:
                    return new Vector3(0, 0, -1);
                case Base6Directions.Direction.Left:
                    return new Vector3(-1, 0, 0);
                case Base6Directions.Direction.Right:
                    return new Vector3(1, 0, 0);
                case Base6Directions.Direction.Up:
                    return new Vector3(0, 1, 0);
                case Base6Directions.Direction.Down:
                    return new Vector3(0, -1, 0);
                default:
                    throw new ArgumentException("Invalid Base6Direction value");
            }
        }

        public Vector3 CalculateRightVector() {
            MyBlockOrientation blockOrientation = Gyro.Orientation;

            // extract Forward and Up directions as Base6Directions
            Base6Directions.Direction forwardDirection = blockOrientation.Forward;
            Base6Directions.Direction upDirection = blockOrientation.Up;

            // convert to Vector3
            Vector3 forwardVector = Base6DirectionToVector3(forwardDirection);
            Vector3 upVector = Base6DirectionToVector3(upDirection);

            // calculate Right vector using cross product
            Vector3 rightVector = Vector3.Cross(forwardVector, upVector);
            return rightVector;
        }
    }
}
