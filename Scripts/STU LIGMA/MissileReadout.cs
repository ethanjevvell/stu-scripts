using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class MissileReadout : STUDisplay {

            public Missile Missile { get; set; }

            public MissileReadout(IMyTextSurface surface, Missile missile) : base(surface, "Monospace", 1f) {
                Missile = missile;
            }

        }
    }
}
