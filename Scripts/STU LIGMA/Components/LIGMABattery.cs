
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class LIGMABattery {

            public IMyBatteryBlock Battery { get; set; }

            public LIGMABattery(IMyBatteryBlock battery) {
                Battery = battery;
            }

        }
    }
}
