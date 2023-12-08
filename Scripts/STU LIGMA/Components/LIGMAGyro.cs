using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class LIGMAGyro {

            public IMyGyro Gyro { get; set; }

            public LIGMAGyro(IMyGyro gyro) {
                Gyro = gyro;
            }

            public void SetYaw(double yaw) {
                Gyro.Yaw = (float)yaw;
            }

            public void SetPitch(double pitch) {
                Gyro.Pitch = (float)pitch;
            }

            public void SetRoll(double roll) {
                Gyro.Roll = (float)roll;
            }

        }
    }
}
