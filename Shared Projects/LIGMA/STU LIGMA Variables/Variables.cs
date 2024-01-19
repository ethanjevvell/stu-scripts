using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public static class LIGMA_VARIABLES {
            public const string LIGMA_VEHICLE_NAME = "LIGMA-I";
            public const string LIGMA_RECONNOITERER_NAME = "SDC-3";
            public const string LIGMA_VEHICLE_BROADCASTER = "LIGMA_VEHICLE_BROADCASTER";
            public const string LIGMA_MISSION_CONTROL_BROADCASTER = "LIGMA_MISSION_CONTROL_BROADCASTER";
            public const string LIGMA_RECONNOITERER_BROADCASTER = "LIGMA_RECONNOITERER_BROADCASTER";

            public static class COMMANDS {
                public const string Launch = "Launch";
                public const string Detonate = "Detonate";
                public const string UpdateTargetData = "UpdateTargetData";
                public const string Test = "Test";
            }

            public struct Planet {
                public double Radius;
                public Vector3D Center;
            }

            public static Dictionary<string, Planet> CelestialBodies = new Dictionary<string, Planet> {
            {
                "TestEarth", new Planet {
                    Radius = 61050.39,
                    Center = new Vector3D(0, 0, 0)
                }
            },
            {
                "Luna", new Planet {
                    Radius = 9453.8439,
                    Center = new Vector3D(16400.0530046 ,  136405.82841528, -113627.17741361)
                }
            }
        };

        }
    }
}
