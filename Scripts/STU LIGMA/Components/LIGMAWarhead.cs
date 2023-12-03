using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class LIGMAWarhead {

            IMyWarhead Warhead { get; set; }

            public LIGMAWarhead(IMyWarhead warhead) {
                Warhead = warhead;
            }

            public void Arm() {
                Warhead.IsArmed = true;
            }

            public void Detonate() {
                Warhead.Detonate();
            }

        }
    }
}
