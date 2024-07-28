using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class STUGalacticMap {

            public struct Planet {
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
            };
        }
    }
}
