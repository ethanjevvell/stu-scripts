
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class STUBattery {

            public IMyBatteryBlock Battery { get; set; }

            public STUBattery(IMyBatteryBlock battery) {
                Battery = battery;
            }

        }
    }
}
