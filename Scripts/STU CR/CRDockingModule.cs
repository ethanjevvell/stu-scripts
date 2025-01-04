using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class CRDockingModule
        {
            // variables
            public enum DockingModuleStates
            {
                Idle,
                AuxiliaryHardwareReset,
                Ready,
            }
            public DockingModuleStates CurrentDockingModuleState { get; set; }

            public static IMyMotorStator GangwayHinge { get; set; }
            public static IMyShipMergeBlock MergeBlock { get; set; }
            public static IMyMotorStator MainDockHinge1 { get; set; }
            public static IMyMotorStator MainDockHinge2 { get; set; }
            public static IMyPistonBase MainDockPiston { get; set; }
            public static IMyShipConnector MainDockConnector { get; set; }
            private static Vector3D CBTLineUpPosition { get; set; }
            private static Vector3D CBTRollReference { get; set; }
            private static Vector3D CBTFinalDockingPosition { get; set; }
            private static MatrixD ThisGridWorldMatrix { get; set; }
            
            public bool DockRequestReceivedFlag = false;

            // constructor
            public CRDockingModule(IMyMotorStator gangwayHinge, IMyMotorStator mainDockHinge1, IMyMotorStator mainDockHinge2, IMyPistonBase piston, IMyShipMergeBlock mergeBlock, IMyShipConnector connector)
            {
                CurrentDockingModuleState = DockingModuleStates.Idle;

                GangwayHinge = gangwayHinge;
                MainDockHinge1 = mainDockHinge1;
                MainDockHinge2 = mainDockHinge2;
                MainDockPiston = piston;
                MergeBlock = mergeBlock;
                MainDockConnector = connector;
            }

            // state machine
            public void UpdateDockingModule()
            {
                ThisGridWorldMatrix = CR.Me.CubeGrid.WorldMatrix;
                // refactor the lines below so that the offset is calculated based on the merge block's orientation, rather than the PB's orientation
                // fix later
                CBTLineUpPosition = CR.MergeBlock.GetPosition() + (CR.MergeBlock.WorldMatrix.Backward * 120) + (CR.MergeBlock.WorldMatrix.Right * 5);
                CBTRollReference = CR.MergeBlock.GetPosition() + (CR.MergeBlock.WorldMatrix.Right * 5) + (CR.MergeBlock.WorldMatrix.Backward * 120) + (CR.MergeBlock.WorldMatrix.Up * 120);
                CBTFinalDockingPosition = CR.MergeBlock.GetPosition() + (CR.MergeBlock.WorldMatrix.Right * 5);
                
                switch (CurrentDockingModuleState)
                {
                    case DockingModuleStates.Idle:
                        if (DockRequestReceivedFlag)
                        {
                            CR.AddToLogQueue("Docking request received", STULogType.INFO);
                            DockRequestReceivedFlag = false;
                            CurrentDockingModuleState = DockingModuleStates.AuxiliaryHardwareReset;
                        }
                        if (!CR.MergeBlock.IsConnected)
                        {
                            CR.GangwayHinge.TargetVelocityRPM = -1f;
                        }
                        break;
                    case DockingModuleStates.AuxiliaryHardwareReset:
                        CR.AddToLogQueue("Resetting auxiliary hardware...", STULogType.INFO);

                        GangwayHinge.TargetVelocityRad = Math.Abs(GangwayHinge.TargetVelocityRad) * -1;
                        MainDockConnector.Disconnect();
                        MainDockHinge1.TargetVelocityRad = Math.Abs(MainDockHinge1.TargetVelocityRad) * -1;
                        MainDockHinge2.TargetVelocityRad = Math.Abs(MainDockHinge2.TargetVelocityRad) * -1;
                        MainDockPiston.Velocity = Math.Abs(MainDockPiston.Velocity) * -1;
                        
                        CR.AddToLogQueue($"Hinge 1 position: {MainDockHinge1.Angle}", STULogType.INFO);
                        CR.AddToLogQueue($"Hinge 2 position: {MainDockHinge2.Angle}", STULogType.INFO);
                        CR.AddToLogQueue($"Piston position: {MainDockPiston.CurrentPosition}", STULogType.INFO);
                        CR.AddToLogQueue($"Gangway position: {GangwayHinge.Angle}", STULogType.INFO);
                        CR.AddToLogQueue("");
                        if (Math.Abs(MainDockHinge1.Angle) < 0.1 && Math.Abs(MainDockHinge2.Angle) - Math.PI/2 < 0.1 && MainDockPiston.CurrentPosition < 0.1 && Math.Abs(GangwayHinge.Angle) < 0.1)
                        {
                            CR.AddToLogQueue("Auxiliary hardware reset complete. Ready for docking...", STULogType.INFO);
                            CR.CreateBroadcast($"POSITION " +
                                $"{CBTLineUpPosition.X} " +
                                $"{CBTLineUpPosition.Y} " +
                                $"{CBTLineUpPosition.Z} " +
                                $"{CBTRollReference.X} " +
                                $"{CBTRollReference.Y} " +
                                $"{CBTRollReference.Z} " +
                                $"{CBTFinalDockingPosition.X} " +
                                $"{CBTFinalDockingPosition.Y} " +
                                $"{CBTFinalDockingPosition.Z} " +
                                $"EOT");
                            CR.CreateBroadcast("READY", false, STULogType.INFO);
                            CurrentDockingModuleState = DockingModuleStates.Ready;
                        }
                        break;
                    case DockingModuleStates.Ready:
                        if (MergeBlock.IsConnected)
                        {
                            CR.AddToLogQueue("Docking complete", STULogType.OK);
                            CR.CreateBroadcast("Docking complete", false, STULogType.OK);
                            CR.GangwayHinge.TargetVelocityRPM = GangwayHinge.TargetVelocityRPM * -1;
                            CurrentDockingModuleState = DockingModuleStates.Idle;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
    
}
