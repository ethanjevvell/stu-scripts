using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        Dictionary<string, BoundingSphere> CelestialBodies;

        public Program() {
            CelestialBodies = new Dictionary<string, BoundingSphere> {
                { "Luna", new BoundingSphere(new Vector3D(16400.0530046,  136405.82841528, -113627.17741361), 9453) }
            };
        }

        public void Main() {
            Ray ray = new Ray(Me.GetPosition(), Me.WorldMatrix.Forward);
            var intersection = ray.Intersects(CelestialBodies["Luna"]);
            if (intersection != null) {
                Echo("Intersects");
            } else {
                Echo("Does not intersect");
            }
        }

    }
}
