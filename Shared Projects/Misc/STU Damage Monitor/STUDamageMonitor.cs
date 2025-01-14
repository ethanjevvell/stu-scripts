using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    partial class Program {
        public class STUDamageMonitor {

            IEnumerator<bool> _damageMonitorStateMachine;
            IMyCubeBlock _tempBlock;

            public List<IMyCubeBlock> HealthyBlocks { get; private set; }
            public List<IMyCubeBlock> DamagedBlocks { get; private set; }

            public STUDamageMonitor(IMyGridTerminalSystem grid, IMyProgrammableBlock me) {
                List<IMyCubeBlock> allBlocks = new List<IMyCubeBlock>();
                grid.GetBlocksOfType(allBlocks, block => block.CubeGrid == me.CubeGrid);
                HealthyBlocks = new List<IMyCubeBlock>();
                DamagedBlocks = new List<IMyCubeBlock>();
                foreach (IMyCubeBlock block in allBlocks) {
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
