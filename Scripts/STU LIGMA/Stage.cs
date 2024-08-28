using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program {

        public class Stage {

            public IMyThrust[] ForwardThrusters;
            public IMyThrust[] ReverseThrusters;
            public IMyThrust[] LateralThrusters;

            public IMyShipMergeBlock MergeBlock;

            public IMyWarhead[] Warheads;

            public Stage(IMyGridTerminalSystem grid, string stageKey) {
                LIGMA.CreateOkBroadcast($"Initializing {stageKey.ToUpper()} stage thrusters and merge block");
                ForwardThrusters = FindThrusterBlockGroup(grid, stageKey.ToUpper(), "FORWARD");
                ReverseThrusters = FindThrusterBlockGroup(grid, stageKey.ToUpper(), "REVERSE");
                LateralThrusters = FindThrusterBlockGroup(grid, stageKey.ToUpper(), "LATERAL");
                Warheads = FindStageWarheads(grid, stageKey.ToUpper());
                MergeBlock = FindStageMergeBlock(grid, stageKey);
            }

            public void ToggleForwardThrusters(bool on) {
                for (var i = 0; i < ForwardThrusters.Length; i++) {
                    ForwardThrusters[i].Enabled = on;
                }
            }

            public void ToggleReverseThrusters(bool on) {
                for (var i = 0; i < ReverseThrusters.Length; i++) {
                    ReverseThrusters[i].Enabled = on;
                }
            }

            public void ToggleLateralThrusters(bool on) {
                for (var i = 0; i < LateralThrusters.Length; i++) {
                    LateralThrusters[i].Enabled = on;
                }
            }

            public void TriggerDisenageBurn(bool on) {
                for (var i = 0; i < ReverseThrusters.Length; i++) {
                    ReverseThrusters[i].ThrustOverridePercentage = 1.0f;
                }
            }

            public void DisconnectMergeBlock() {
                MergeBlock.Enabled = false;
            }

            public void TriggerDetonationCountdown() {
                for (var i = 0; i < Warheads.Length; i++) {
                    Warheads[i].IsArmed = true;
                    Warheads[i].DetonationTime = 5;
                    Warheads[i].StartCountdown();
                }
            }

            private IMyThrust[] FindThrusterBlockGroup(IMyGridTerminalSystem grid, string stageKey, string direction) {

                List<IMyTerminalBlock> thrusters = new List<IMyTerminalBlock>();
                try {
                    grid.GetBlockGroupWithName($"{stageKey}_STAGE_{direction.ToUpper()}_THRUSTERS").GetBlocks(thrusters);
                } catch (Exception e) {
                    if (thrusters.Count == 0) {
                        LIGMA.CreateWarningBroadcast($"No {direction} thrusters found for {stageKey.ToUpper()} stage!");
                        return new IMyThrust[0] { };
                    } else {
                        LIGMA.CreateFatalErrorBroadcast($"Error finding {direction} thrusters for {stageKey.ToUpper()} stage: {e}");
                    }
                }

                IMyThrust[] thrustArray = new IMyThrust[thrusters.Count];

                for (var i = 0; i < thrusters.Count; i++) {
                    thrustArray[i] = (IMyThrust)thrusters[i];
                }

                LIGMA.CreateWarningBroadcast($"{thrusters.Count} {direction} thrusters found for {stageKey.ToUpper()} stage!");

                return thrustArray;
            }

            private IMyWarhead[] FindStageWarheads(IMyGridTerminalSystem grid, string stageKey) {

                List<IMyTerminalBlock> warheads = new List<IMyTerminalBlock>();
                LIGMA.CreateOkBroadcast($"Finding warheads for {stageKey.ToUpper()} stage");
                try {
                    grid.GetBlockGroupWithName($"{stageKey}_STAGE_WARHEADS").GetBlocks(warheads);
                } catch (Exception e) {
                    if (warheads.Count == 0) {
                        LIGMA.CreateFatalErrorBroadcast($"No warheads found for {stageKey.ToUpper()} stage!");
                    } else {
                        LIGMA.CreateFatalErrorBroadcast($"Error finding warheads for {stageKey.ToUpper()} stage: {e}");
                    }
                }

                IMyWarhead[] warheadArray = new IMyWarhead[warheads.Count];

                for (var i = 0; i < warheads.Count; i++) {
                    warheadArray[i] = (IMyWarhead)warheads[i];
                }

                LIGMA.CreateWarningBroadcast($"{warheads.Count} warheads found for {stageKey.ToUpper()} stage!");

                return warheadArray;
            }

            private IMyShipMergeBlock FindStageMergeBlock(IMyGridTerminalSystem grid, string stageKey) {

                string mergeBlockName = "";

                switch (stageKey) {
                    case "LAUNCH":
                        mergeBlockName = "LAUNCH_TO_FLIGHT_MERGE_BLOCK";
                        break;
                    case "FLIGHT":
                        mergeBlockName = "FLIGHT_TO_TERMINAL_MERGE_BLOCK";
                        break;
                    case "TERMINAL":
                        mergeBlockName = "TERMINAL_TO_FLIGHT_MERGE_BLOCK";
                        break;
                    default:
                        LIGMA.CreateFatalErrorBroadcast($"Error finding {stageKey.ToUpper()} stage merge block");
                        break;
                }

                IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)grid.GetBlockWithName(mergeBlockName);

                if (mergeBlock == null) {
                    LIGMA.CreateFatalErrorBroadcast($"Error finding {stageKey.ToUpper()} stage merge block");
                }

                return mergeBlock;

            }

            public static IMyThrust[] RemoveThrusters(IMyThrust[] allThrusters, IMyThrust[] thrustersToRemove) {
                List<IMyThrust> allThrustersList = new List<IMyThrust>(allThrusters);
                List<IMyThrust> thrustersToRemoveList = new List<IMyThrust>(thrustersToRemove);
                for (var i = 0; i < thrustersToRemoveList.Count; i++) {
                    allThrustersList.Remove(thrustersToRemoveList[i]);
                }
                return allThrustersList.ToArray();
            }

        }

    }
}
