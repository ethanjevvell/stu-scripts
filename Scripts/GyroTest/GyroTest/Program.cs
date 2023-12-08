using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        bool halt = false;
        
        public IMyGyro Gyro;

        public Vector3D UP = new Vector3D(111754.91, 132105.46, 5841669.28);
        public Vector3D RIGHT = new Vector3D(111766.69, 132116.41, 5841675.86);
        public Vector3D BACK = new Vector3D(111760.83, 132105.97, 5841682.11);
        public Vector3D BIG_TEST = new Vector3D(110880, 133085, 5841330.01);

        Vector3D gyroForwardVec = new Vector3D();
        Vector3D gyroUpVec = new Vector3D();
        Vector3D gyroRightVec = new Vector3D();

        Vector3D gyroForwardVecAbs = new Vector3D();
        Vector3D gyroUpVecAbs = new Vector3D();
        Vector3D gyroRightVecAbs = new Vector3D();

        double smoothingFactor = 0.75;

        public Program()
        {

            Gyro = GridTerminalSystem.GetBlockWithName("Gyroscope") as IMyGyro;

        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            
            // gather measurements about the world and the gyro
            Vector3D gyroPosition = Gyro.GetPosition();
            Vector3D targetPosition = BIG_TEST;
            Vector3D directionVectorAbsolute = targetPosition - gyroPosition;
            Vector3D directionVectorNormalized = Vector3D.Normalize(targetPosition - gyroPosition);

            Echo($"gyroPosition: {gyroPosition}\n" +
                $"targetPosition: {targetPosition}\n" +
                $"directionVectorAbs: {directionVectorAbsolute}\n" +
                $"directionVectorNorm: {directionVectorNormalized}\n");

            // get gyro's forward, up, and right vectors w.r.t the gyro itself
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
            double rollAngle = CalculateRollAngle(gyroUpVecAbs, gyroForwardVec, new Vector3D(1,1,1));
            Echo($"yawAngle = {yawAngle}\n" +
                $"pitchAngle = {pitchAngle}\n" +
                $"rollAngle = {rollAngle}\n");

            //Roll(Gyro, rollAngle * smoothingFactor);
            Pitch(Gyro, pitchAngle * smoothingFactor);
            Yaw(Gyro, yawAngle * smoothingFactor);

            //if (Math.Abs(rollAngle) < 0.01) { Roll(Gyro, 0); }
            if (Math.Abs(pitchAngle) < 0.01) { Pitch(Gyro, 0); }
            if (Math.Abs(yawAngle) < 0.01) { Yaw(Gyro, 0); }

            //Orient(rollAngle, pitchAngle, yawAngle);

            //Roll(Gyro, rollAngle * smoothingFactor * 0.5);
            //if (Math.Abs(rollAngle) < 0.01) { Roll(Gyro, 0); }

            if (argument.Contains("reset"))
            {
                Reset(Gyro);
            }

            //if (argument.Substring(0, 1) == "r")
            //{ Roll(Gyro, float.Parse(argument.Substring(1, argument.Length - 1))); }

            //if (argument.Substring(0, 1) == "p")
            //{ Pitch(Gyro, float.Parse(argument.Substring(1, argument.Length - 1))); }

            //if (argument.Substring(0, 1) == "y")
            //{ Yaw(Gyro, float.Parse(argument.Substring(1, argument.Length - 1))); }

            if (halt) { 
                Echo("Program halting...");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                Reset(Gyro);
            }
        }

        public void Orient(double roll, double pitch, double yaw)
        {
            Pitch(Gyro, pitch * smoothingFactor);
            Yaw(Gyro, yaw*smoothingFactor);
        }

        public double CalculateYawAngle(Vector3D gyroForward, Vector3D gyroUp, Vector3D directionVector)
        {
            // project gyro's directional vectors onto the horizontal plane by removing the up component
            Vector3D forwardHorizontal = Vector3D.Reject(gyroForward, gyroUp);
            Vector3D targetHorizontal = Vector3D.Reject(directionVector, gyroUp);

            // calculate the angle
            double angle = Math.Acos(Vector3D.Dot(forwardHorizontal, targetHorizontal) / (forwardHorizontal.Length() * targetHorizontal.Length()));

            // determine direction of rotation using cross product
            if (Vector3D.Cross(forwardHorizontal, targetHorizontal).Dot(gyroUp) < 0 )
            {
                angle = -angle;
            }

            return angle;
        }

        public double CalculatePitchAngle(Vector3D gyroForward, Vector3D gyroRight, Vector3D directionVector)
        {
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
            if (Vector3D.Cross(forwardOnPlane, targetOnPlane).Dot(gyroRight) < 0)
            {
                angle = -angle;
            }

            return angle;
        }

        public double CalculateRollAngle(Vector3D gyroUp, Vector3D gyroForward, Vector3D referenceUp)
        {
            // project the gyro's up vector and the reference up vector onto a plane defined by gyroForward
            Vector3D gyroUpOnPlane = Vector3D.Reject(gyroUp, gyroForward);
            Vector3D referenceUpOnPlane = Vector3D.Reject(referenceUp, gyroForward);

            // normalize to only consider directions
            gyroUpOnPlane.Normalize();
            referenceUpOnPlane.Normalize();

            // calculate the angle between the two vectors
            double angle = Math.Acos(Vector3D.Dot(gyroUpOnPlane, referenceUpOnPlane));

            // determine the direction of the roll using cross product
            // if the cross product in the direction of gyroForward is negative, invert the angle
            if (Vector3D.Cross(gyroUpOnPlane, referenceUpOnPlane).Dot(referenceUp) < 0)
            {
                angle = -angle;
            }

            return angle;
        }
        public void Reset(IMyGyro gyro)
        {
            Echo("resetting gyro moments to 0");
            gyro.GyroOverride = true;
            gyro.Pitch = 0;
            gyro.Roll = 0;
            gyro.Yaw = 0;
            halt = true;
        }

        public void Pitch(IMyGyro gyro, double pitchSpeed)
        {
            Echo($"pitching at a speed of {pitchSpeed}");
            gyro.Pitch = (float)pitchSpeed;
        }

        public void Roll(IMyGyro gyro, double rollSpeed)
        {
            Echo($"rolling at a speed of {rollSpeed}");
            gyro.Roll = (float)rollSpeed;
        }

        public void Yaw(IMyGyro gyro, double yawSpeed)
        {
            Echo($"yawing at a speed of {yawSpeed}");
            gyro.Yaw = (float)yawSpeed;
        }

        public Vector3 Base6DirectionToVector3(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    return new Vector3(0, 0, -1);
                case Base6Directions.Direction.Backward:
                    return new Vector3(0, 0, 1);
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

        public Vector3 CalculateRightVector()
        {
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
