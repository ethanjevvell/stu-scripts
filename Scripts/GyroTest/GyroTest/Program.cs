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
            
            Vector3D gyroPosition = Gyro.GetPosition();
            Vector3D targetPosition = new Vector3D(111766.69, 132116.41, 5841673.86);
            Vector3D directionVector = Vector3D.Normalize(targetPosition - gyroPosition);

            Echo($"gyroPosition: {gyroPosition}\n" +
                $"targetPosition: {targetPosition}\n" +
                $"directionVector: {directionVector}");

            // get gyro's forward, up, and right vectors
            Vector3D gyroForward = new Vector3D(Base6DirectionToVector3(Gyro.Orientation.Forward));
            Vector3D gyroUp = new Vector3D(Base6DirectionToVector3(Gyro.Orientation.Up));
            Vector3D gyroRight = CalculateRightVector();

            double yawAngle = CalculateYawAngle(gyroForward, gyroUp, directionVector);
            double pitchAngle = CalculatePitchAngle(gyroForward, gyroRight, directionVector);
            double rollAngle = CalculateRollAngle(gyroUp, gyroForward, directionVector);
            Echo($"yawAngle = {yawAngle}\n" +
                $"pitchAngle = {pitchAngle}\n" +
                $"rollAngle = {rollAngle}");

            Yaw(Gyro, (float)0.1);
            if (Math.Abs(yawAngle) < 0.001) { halt = true; }

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

        public void Pitch(IMyGyro gyro, float pitchSpeed)
        {
            Echo($"pitching at a speed of {pitchSpeed}");
            gyro.Pitch = pitchSpeed;
        }

        public void Roll(IMyGyro gyro, float rollSpeed)
        {
            Echo($"rolling at a speed of {rollSpeed}");
            gyro.Roll = rollSpeed;
        }

        public void Yaw(IMyGyro gyro, float yawSpeed)
        {
            Echo($"yawing at a speed of {yawSpeed}");
            gyro.Yaw = yawSpeed;
        }

        public Vector3 Base6DirectionToVector3(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    return new Vector3(0, 0, 1); // look into Vector3I.Forward if this causes trouble later
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
        //public Vector3D GetCurrentOrientation(IMyGyro gyro)
        //{

        //}

        //public Vector2D ParseTargetOrientation(Vector3D vecTarget)
        //{

        //}

        //public double CalculateOrientationDeltaX(Vector3D vecCurrent, Vector3D vecTarget)
        //{

        //}

        //public double CalculateOrientationDeltaZ(Vector3D vecCurrent, Vector3D vecTarget)
        //{

        //}
    }
}
