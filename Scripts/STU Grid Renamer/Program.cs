using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {

        public void Main(string argument) {

            if (string.IsNullOrEmpty(argument)) {
                return;
            }

            // Loop through every block in the grid and prepend argument to the name
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, block => block.CubeGrid == Me.CubeGrid);
            foreach (var block in blocks) {
                if (!block.CustomName.Contains($"[{argument}]")) {
                    // check if it contains another [argument] and remove it    
                    if (block.CustomName.Contains("[") && block.CustomName.Contains("]")) {
                        block.CustomName = block.CustomName.Substring(block.CustomName.IndexOf("]") + 1);
                    }
                    block.CustomName = $"[{argument}] " + block.CustomName;
                }
            }

        }

    }
}
