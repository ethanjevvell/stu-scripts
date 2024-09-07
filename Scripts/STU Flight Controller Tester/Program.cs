using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        STUFlightController FlightController;
        IMyThrust[] Thrusters;
        IMyRemoteControl RemoteControl;
        IMyGyro[] Gyros;

        LogLCD LogScreen;

        public Program() {
            Thrusters = FindThrusters();
            RemoteControl = FindRemoteControl();
            Gyros = FindGyros();
            LogScreen = FindLogScreen();
            FlightController = new STUFlightController(GridTerminalSystem, RemoteControl, Thrusters, Gyros);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            FlightController.RelinquishGyroControl();
        }

        public void Main() {
            FlightController.UpdateState();
            // FlightController.OrbitPlanet();
            if (STUFlightController.FlightLogs.Count > 0) {
                while (STUFlightController.FlightLogs.Count > 0) {
                    LogScreen.FlightLogs.Enqueue(STUFlightController.FlightLogs.Dequeue());
                }
                LogScreen.StartFrame();
                LogScreen.WriteWrappableLogs(LogScreen.FlightLogs);
                LogScreen.EndAndPaintFrame();
            }
        }

        private IMyThrust[] FindThrusters() {
            List<IMyThrust> thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrusters, block => block.CubeGrid == Me.CubeGrid);
            return thrusters.ToArray();
        }

        private IMyRemoteControl FindRemoteControl() {
            List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remoteControls, block => block.CubeGrid == Me.CubeGrid);
            return remoteControls[0];
        }

        private IMyGyro[] FindGyros() {
            List<IMyGyro> gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(gyros, block => block.CubeGrid == Me.CubeGrid);
            return gyros.ToArray();
        }

        private LogLCD FindLogScreen() {
            // find block with name "LogLCD"
            IMyTerminalBlock logScreen = GridTerminalSystem.GetBlockWithName("LogLCD");
            return new LogLCD(logScreen, 0, "Monospace", 0.7f);
        }

    }
}
