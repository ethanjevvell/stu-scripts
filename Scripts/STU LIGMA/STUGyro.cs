using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class STUGyro {

            public IMyGyro Gyro { get; set; }

            public STUGyro(IMyGyro gyro) {
                Gyro = gyro;
            }
        }
    }
}
