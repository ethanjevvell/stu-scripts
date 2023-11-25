
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class STUFuelTank {

            public IMyGasTank Tank { get; set; }

            public STUFuelTank(IMyGasTank tank) {
                Tank = tank;
            }

        }
    }
}
