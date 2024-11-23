using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class STUGalacticMap {

            public static Dictionary<string, Vector3D> Waypoints = new Dictionary<string, Vector3D>
            {
                { "CBT", new Vector3D(99756.85,158304.72,5859075.56) },
                { "CBT2", new Vector3D(100033.4,158844.99,5858882.1) },
            };

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
            },
                {
                    "Crait", new Planet {
                        Name = "Crait",
                        Radius = 40644.8713,
                        Center = new Vector3D(415363, 125322, -94326)
                    }
                }
            };

            /// <summary>
            /// Finds the planet that contains the given point, with a default detection buffer of 1000m
            /// Returns null if no planet contains the point
            /// </summary>
            /// <param name="point"></param>
            /// <param name="detectionBuffer"></param>
            /// <returns></returns>
            public static Planet? GetPlanetOfPoint(Vector3D point, double detectionBuffer = 1000) {
                foreach (var kvp in CelestialBodies) {
                    Planet planet = kvp.Value;
                    BoundingSphereD sphere = new BoundingSphereD(planet.Center, planet.Radius + detectionBuffer);
                    // if the point is inside the planet"s detection sphere or intersects it, it is on the planet
                    if (sphere.Contains(point) == ContainmentType.Contains || sphere.Contains(point) == ContainmentType.Intersects) {
                        return planet;
                    }
                }
                return null;
            }

        }
    }
}
