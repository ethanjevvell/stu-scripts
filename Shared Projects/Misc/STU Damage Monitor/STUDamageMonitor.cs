using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public class STUDamageMonitor {

            IEnumerator<bool> _damageMonitorStateMachine;
            IMyTerminalBlock _tempBlock;

            public List<IMyTerminalBlock> HealthyBlocks { get; private set; }
            public List<IMyTerminalBlock> DamagedBlocks { get; private set; }

            public STUDamageMonitor(IMyGridTerminalSystem grid, IMyProgrammableBlock me) {
                List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
                grid.GetBlocksOfType<IMyTerminalBlock>(allBlocks, block => block.CubeGrid == me.CubeGrid);
                HealthyBlocks = new List<IMyTerminalBlock>();
                DamagedBlocks = new List<IMyTerminalBlock>();
                foreach (IMyTerminalBlock block in allBlocks) {
                    if (block.IsFunctional) {
                        HealthyBlocks.Add(block);
                    } else {
                        DamagedBlocks.Add(block);
                    }
                }
            }

            public void MonitorDamage() {
                if (_damageMonitorStateMachine == null) {
                    _damageMonitorStateMachine = MonitorDamageCoroutine().GetEnumerator();
                }

                if (_damageMonitorStateMachine.MoveNext()) {
                    return;
                }

                _damageMonitorStateMachine.Dispose();
                _damageMonitorStateMachine = null;
            }

            IEnumerable<bool> MonitorDamageCoroutine() {
                // move through damaged blocks and check if they are now healthy
                for (int i = 0; i < DamagedBlocks.Count; i++) {
                    _tempBlock = DamagedBlocks[i];
                    // If the block is missing from the world now, remove it from the list
                    if (_tempBlock.Closed) {
                        DamagedBlocks.RemoveAt(i);
                        i--;
                        continue;
                    }
                    if (_tempBlock.IsFunctional) {
                        HealthyBlocks.Add(_tempBlock);
                        DamagedBlocks.RemoveAt(i);
                        i--;
                    }
                    yield return true;
                }
                // move through healthy blocks and check if they are now damaged
                for (int j = 0; j < HealthyBlocks.Count; j++) {
                    _tempBlock = HealthyBlocks[j];
                    if (_tempBlock.Closed) {
                        HealthyBlocks.RemoveAt(j);
                        j--;
                        continue;
                    }
                    if (!_tempBlock.IsFunctional) {
                        DamagedBlocks.Add(_tempBlock);
                        HealthyBlocks.RemoveAt(j);
                        j--;
                    }
                    yield return true;
                }
                // if no changes, wait for next tick
                yield return true;
            }

        }
    }
}
