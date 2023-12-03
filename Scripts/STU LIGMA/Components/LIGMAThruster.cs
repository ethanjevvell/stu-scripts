
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class LIGMAThruster {

            public IMyThrust Thruster { get; set; }

            public LIGMAThruster(IMyThrust thruster) {
                Thruster = thruster;
            }

        }
    }
}
