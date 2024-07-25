using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    partial class Program
    {
        public static class CBT_VARIABLES
        {
            public const string CBT_VEHICLE_NAME = "CBT";

            public const double PLANETARY_DETECTION_BUFFER = 2000;

            public static class COMMANDS
            {
                public const string Stop = "Stop";
            }

            public struct Planet
            {
                public string Name;
                public double Radius;
                public Vector3D Center;
            }

            public static Dictionary<string, Planet> CelestialBodies = new Dictionary<string, Planet> {
            {
                "TestEarth", new Planet {
                    Name = "TestEarth",
                    Radius = 61050.39,
                    Center = new Vector3D(0, 0, 0)
                }
            },
            {
                "Luna", new Planet {
                    Name = "Luna",
                    Radius = 9453.8439,
                    Center = new Vector3D(16400.0530046 ,  136405.82841528, -113627.17741361)
                }
            },
                {
                   "Mars", new Planet {
                    Name = "Mars",
                    Radius = 62763.4881,
                    Center = new Vector3D(1031060.3327, 131094.9846, 1631139.8156)
                   }
                }
        }
    }
}
