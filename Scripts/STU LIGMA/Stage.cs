using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {

        public class Stage
        {

            public IMyThrust[] ForwardThrusters;
            public IMyThrust[] ReverseThrusters;
            public IMyThrust[] LateralThrusters;

            public IMyGasTank[] HydrogenTanks;
            public IMyShipMergeBlock MergeBlock;
            public IMyWarhead[] Warheads;

            public double CurrentFuel = 0;
            public double FuelCapacity = 0;

            public Stage(IMyGridTerminalSystem grid, string stageKey)
            {
                LIGMA.CreateOkBroadcast($"Initializing {stageKey.ToUpper()} stage thrusters and merge block");
                ForwardThrusters = FindThrusterBlockGroup(grid, stageKey.ToUpper(), "FORWARD");
                ReverseThrusters = FindThrusterBlockGroup(grid, stageKey.ToUpper(), "REVERSE");
                LateralThrusters = FindThrusterBlockGroup(grid, stageKey.ToUpper(), "LATERAL");
                HydrogenTanks = FindHydrogenTanks(grid, stageKey.ToUpper());
                LIGMA.CreateOkBroadcast($"{HydrogenTanks.Length} tanks in {stageKey}");
                FuelCapacity = MeasureFuelCapcity(HydrogenTanks);
                Warheads = FindStageWarheads(grid, stageKey.ToUpper());
                MergeBlock = FindStageMergeBlock(grid, stageKey.ToUpper());
                LIGMA.CreateOkBroadcast($"{stageKey.ToUpper()} stage fuel capacity: {FuelCapacity}");
            }

            public void ToggleForwardThrusters(bool on)
            {
                for (var i = 0; i < ForwardThrusters.Length; i++)
                {
                    ForwardThrusters[i].Enabled = on;
                }
            }

            public void ToggleReverseThrusters(bool on)
            {
                for (var i = 0; i < ReverseThrusters.Length; i++)
                {
                    ReverseThrusters[i].Enabled = on;
                }
            }

            public void ToggleLateralThrusters(bool on)
            {
                for (var i = 0; i < LateralThrusters.Length; i++)
                {
                    LateralThrusters[i].Enabled = on;
                }
            }

            public void TriggerDisenageBurn()
            {
                for (var i = 0; i < ReverseThrusters.Length; i++)
                {
                    ReverseThrusters[i].ThrustOverridePercentage = 1.0f;
                }
            }

            public void DisconnectMergeBlock()
            {
                MergeBlock.Enabled = false;
            }

            public void TriggerDetonationCountdown()
            {
                for (var i = 0; i < Warheads.Length; i++)
                {
                    Warheads[i].IsArmed = true;
                    Warheads[i].DetonationTime = 5;
                    Warheads[i].StartCountdown();
                }
            }

            private IMyThrust[] FindThrusterBlockGroup(IMyGridTerminalSystem grid, string stageKey, string direction)
            {

                List<IMyTerminalBlock> thrusters = new List<IMyTerminalBlock>();
                try
                {
                    grid.GetBlockGroupWithName($"{stageKey}_STAGE_{direction.ToUpper()}_THRUSTERS").GetBlocks(thrusters);
                }
                catch (Exception e)
                {
                    if (thrusters.Count == 0)
                    {
                        LIGMA.CreateWarningBroadcast($"No {direction} thrusters found for {stageKey.ToUpper()} stage!");
                        return new IMyThrust[0] { };
                    }
                    else
                    {
                        LIGMA.CreateFatalErrorBroadcast($"Error finding {direction} thrusters for {stageKey.ToUpper()} stage: {e}");
                    }
                }

                IMyThrust[] thrustArray = new IMyThrust[thrusters.Count];

                for (var i = 0; i < thrusters.Count; i++)
                {
                    thrustArray[i] = (IMyThrust)thrusters[i];
                }

                LIGMA.CreateWarningBroadcast($"{thrusters.Count} {direction} thrusters found for {stageKey.ToUpper()} stage!");

                return thrustArray;
            }

            private IMyWarhead[] FindStageWarheads(IMyGridTerminalSystem grid, string stageKey)
            {

                List<IMyTerminalBlock> warheads = new List<IMyTerminalBlock>();
                LIGMA.CreateOkBroadcast($"Finding warheads for {stageKey.ToUpper()} stage");
                try
                {
                    grid.GetBlockGroupWithName($"{stageKey}_STAGE_WARHEADS").GetBlocks(warheads);
                }
                catch (Exception e)
                {
                    if (warheads.Count == 0)
                    {
                        LIGMA.CreateFatalErrorBroadcast($"No warheads found for {stageKey.ToUpper()} stage!");
                    }
                    else
                    {
                        LIGMA.CreateFatalErrorBroadcast($"Error finding warheads for {stageKey.ToUpper()} stage: {e}");
                    }
                }

                IMyWarhead[] warheadArray = new IMyWarhead[warheads.Count];

                for (var i = 0; i < warheads.Count; i++)
                {
                    warheadArray[i] = (IMyWarhead)warheads[i];
                }

                LIGMA.CreateWarningBroadcast($"{warheads.Count} warheads found for {stageKey.ToUpper()} stage!");

                return warheadArray;
            }

            private IMyShipMergeBlock FindStageMergeBlock(IMyGridTerminalSystem grid, string stageKey)
            {

                string mergeBlockName = "";

                switch (stageKey)
                {
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

                if (mergeBlock == null)
                {
                    LIGMA.CreateFatalErrorBroadcast($"Error finding {stageKey.ToUpper()} stage merge block");
                }

                return mergeBlock;

            }

            private IMyGasTank[] FindHydrogenTanks(IMyGridTerminalSystem grid, string stageKey)
            {

                List<IMyTerminalBlock> tanks = new List<IMyTerminalBlock>();

                try
                {
                    grid.GetBlockGroupWithName($"{stageKey}_STAGE_HYDROGEN_TANKS").GetBlocks(tanks);
                }
                catch (Exception e)
                {
                    if (tanks.Count == 0)
                    {
                        LIGMA.CreateFatalErrorBroadcast($"No hydrogen tanks found for {stageKey.ToUpper()} stage!");
                    }
                    else
                    {
                        LIGMA.CreateFatalErrorBroadcast($"Error finding hydrogen tanks for {stageKey.ToUpper()} stage: {e}");
                    }
                }

                IMyGasTank[] tanksArray = new IMyGasTank[tanks.Count];

                for (int i = 0; i < tanks.Count; i++)
                {
                    tanksArray[i] = tanks[i] as IMyGasTank;
                }

                LIGMA.CreateWarningBroadcast($"{tanks.Count} hydrogen tanks found for {stageKey.ToUpper()} stage!");

                return tanksArray;

            }

            private double MeasureFuelCapcity(IMyGasTank[] tanks)
            {
                double capacity = 0;
                for (var i = 0; i < tanks.Length; i++)
                {
                    capacity += tanks[i].Capacity;
                }
                return capacity;
            }

            /// <summary>
            /// Remove thrusters from allThrusters that are in thrustersToRemove, return new array containing remaining thrusters
            /// </summary>
            /// <param name="allThrusters"></param>
            /// <param name="thrustersToRemove"></param>
            /// <returns></returns>
            public static IMyThrust[] RemoveThrusters(IMyThrust[] allThrusters, IMyThrust[] thrustersToRemove)
            {
                List<IMyThrust> allThrustersList = new List<IMyThrust>(allThrusters);
                List<IMyThrust> thrustersToRemoveList = new List<IMyThrust>(thrustersToRemove);
                for (var i = 0; i < thrustersToRemoveList.Count; i++)
                {
                    allThrustersList.Remove(thrustersToRemoveList[i]);
                }
                return allThrustersList.ToArray();
            }

            /// <summary>
            /// Remove tanks from allTanks that are in tanksToRemove, return new array containing remaining tanks
            /// </summary>
            /// <param name="allTanks"></param>
            /// <param name="tanksToRemove"></param>
            /// <returns></returns>
            public static IMyGasTank[] RemoveHydrogenTanks(IMyGasTank[] allTanks, IMyGasTank[] tanksToRemove)
            {
                List<IMyGasTank> allTanksList = new List<IMyGasTank>(allTanks);
                List<IMyGasTank> tanksToRemoveList = new List<IMyGasTank>(tanksToRemove);
                for (var i = 0; i < tanksToRemoveList.Count; i++)
                {
                    allTanksList.Remove(tanksToRemoveList[i]);
                }
                return allTanksList.ToArray();
            }

            public void MeasureCurrentFuel()
            {
                double currentFuel = 0;
                foreach (IMyGasTank tank in HydrogenTanks)
                {
                    currentFuel += tank.FilledRatio * tank.Capacity;
                }
                CurrentFuel = currentFuel;
            }


        }

    }
}
