using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class LIGMAGyro {

            public IMyGyro Gyro { get; set; }

            public LIGMAGyro(IMyGyro gyro) {
                Gyro = gyro;
            }
        }
    }
}
