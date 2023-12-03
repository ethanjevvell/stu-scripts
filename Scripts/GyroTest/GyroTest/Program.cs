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
        IMyGyro Gyro;

        public MyBlockOrientation orientation;
        public Program()
        {
            
            
            Gyro = GridTerminalSystem.GetBlockWithName("Gyroscope") as IMyGyro;

        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            
            Echo($"{Gyro.Orientation}\n");
            
            if (argument.Contains("reset"))
            {
                Reset(Gyro);
                return;
            }

            if (argument.Substring(0, 1) == "r") 
            { Roll(Gyro, float.Parse(argument.Substring(1,argument.Length - 1))); }

            if (argument.Substring(0,1) == "p")
            { Pitch(Gyro, float.Parse(argument.Substring(1, argument.Length - 1))); }

            if (argument.Substring(0, 1) == "y")
            { Yaw(Gyro, float.Parse(argument.Substring(1, argument.Length - 1))); }
        }


        public void Reset(IMyGyro gyro)
        {
            bool previousOverride = gyro.GyroOverride;
            gyro.GyroOverride = true;
            gyro.Pitch = 0;
            gyro.Roll = 0;
            gyro.Yaw = 0;
            gyro.GyroOverride = previousOverride;
        }

        public void Pitch(IMyGyro gyro, float pitchRPM)
        {
            gyro.Pitch = pitchRPM;
        }

        public void Roll(IMyGyro gyro, float rollRPM)
        {
            gyro.Roll = rollRPM;
        }

        public void Yaw(IMyGyro gyro, float yawRPM)
        {
            gyro.Yaw = yawRPM;
        }
    }
}
