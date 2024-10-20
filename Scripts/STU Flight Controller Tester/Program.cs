using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

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
            LogScreen = new LogLCD(GridTerminalSystem.GetBlockWithName("LogLCD"), 0, "Monospace", 0.7f);
            FlightController = new STUFlightController(GridTerminalSystem, RemoteControl, Me);
            FlightController.RelinquishGyroControl();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            FlightController.GotoAndStopManeuver = new STUFlightController.GotoAndStop(FlightController, new Vector3D(-1739, 3223, -963), 10);
        }

        public void Main() {
            try {
                FlightController.UpdateState();
                FlightController.ReinstateThrusterControl();
                FlightController.MaintainSurfaceAltitude(50);
            } catch (Exception e) {
                STUFlightController.CreateFatalFlightLog(e.Message);
            } finally {
                if (STUFlightController.FlightLogs.Count > 0) {
                    while (STUFlightController.FlightLogs.Count > 0) {
                        LogScreen.FlightLogs.Enqueue(STUFlightController.FlightLogs.Dequeue());
                    }
                    LogScreen.StartFrame();
                    LogScreen.WriteWrappableLogs(LogScreen.FlightLogs);
                    LogScreen.EndAndPaintFrame();
                }
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
