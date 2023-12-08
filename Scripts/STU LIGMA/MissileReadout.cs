using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class MissileReadout : STUDisplay {

            public LIGMA Missile { get; set; }

            public MissileReadout(IMyTerminalBlock block, int displayIndex, LIGMA missile) : base(block, displayIndex, "Monospace", 1f) {
                Missile = missile;
            }

        }
    }
}
