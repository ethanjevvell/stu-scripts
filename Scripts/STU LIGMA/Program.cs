using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        Missile missile;
        MissileReadout display;

        public Program() {
            missile = new Missile(LoadThrusters(), LoadGyros());
            display = new MissileReadout(Me.GetSurface(0), missile);
        }

        public void Main() {
        }

        public STUThruster[] LoadThrusters() {
            List<IMyTerminalBlock> thrusterBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusterBlocks, block => block.CubeGrid == Me.CubeGrid);
            if (thrusterBlocks.Count == 0) {
                throw new Exception("No thrusters found on grid.");
            }
            STUThruster[] thrusters = new STUThruster[thrusterBlocks.Count];
            for (int i = 0; i < thrusterBlocks.Count; i++) {
                thrusters[i] = new STUThruster(thrusterBlocks[i] as IMyThrust);
            }
            return thrusters;
        }

        public STUGyro[] LoadGyros() {
            List<IMyTerminalBlock> gyroBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyroBlocks, block => block.CubeGrid == Me.CubeGrid);
            if (gyroBlocks.Count == 0) {
                throw new Exception("No gyros found on grid.");
            }
            STUGyro[] gyros = new STUGyro[gyroBlocks.Count];
            for (int i = 0; i < gyroBlocks.Count; i++) {
                gyros[i] = new STUGyro(gyroBlocks[i] as IMyGyro);
            }
            return gyros;
        }

    }
}
