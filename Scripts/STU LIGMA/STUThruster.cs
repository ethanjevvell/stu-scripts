
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class STUThruster {

            public IMyThrust Thruster { get; set; }

            public STUThruster(IMyThrust thruster) {
                Thruster = thruster;
            }

        }
    }
}
