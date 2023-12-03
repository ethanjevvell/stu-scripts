
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class LIGMAFuelTank {

            public IMyGasTank Tank { get; set; }

            public LIGMAFuelTank(IMyGasTank tank) {
                Tank = tank;
            }

        }
    }
}
