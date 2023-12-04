
using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript {
    partial class Program {
        public class LIGMAThruster {

            public IMyThrust Thruster { get; set; }

            public LIGMAThruster(IMyThrust thruster) {
                Thruster = thruster;
            }

            public void SetThrust(float overrideValue) {
                // Thrust override is capped at the max effective thrust
                Thruster.ThrustOverride = Math.Min(Thruster.MaxEffectiveThrust, overrideValue);
            }

        }
    }
}
