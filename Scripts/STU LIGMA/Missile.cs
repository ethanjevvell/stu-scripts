namespace IngameScript {
    partial class Program {
        public class Missile {

            public STUThruster[] Thrusters { get; set; }
            public STUGyro[] Gyros { get; set; }

            public Missile(STUThruster[] thrusters, STUGyro[] gyros) {
                Thrusters = thrusters;
                Gyros = gyros;
            }

            public void ToggleThrusters(bool onOrOff) {
                foreach (STUThruster thruster in Thrusters) {
                    thruster.Thruster.Enabled = onOrOff;
                }
            }

            public void ToggleGyros(bool onOrOff) {
                foreach (STUGyro gyro in Gyros) {
                    gyro.Gyro.Enabled = onOrOff;
                }
            }

        }
    }
}
