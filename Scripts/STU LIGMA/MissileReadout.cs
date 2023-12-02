using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class MissileReadout : STUDisplay {

            public Missile Missile { get; set; }

            public MissileReadout(IMyTerminalBlock block, int displayIndex, Missile missile) : base(block, displayIndex, "Monospace", 1f) {
                Missile = missile;
            }

        }
    }
}
