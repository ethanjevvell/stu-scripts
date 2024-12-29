using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    partial class Program
    {
        public class CRDockingModule
        {
            // variables
            public enum DockingModuleStates
            {
                Idle,
                AuxiliaryHardwareReset,
                Ready,
            }
            public DockingModuleStates CurrentDockingModuleState { get; set; }

            
            
            public bool DockRequestReceivedFlag = false;

            // constructor
            public CRDockingModule()
            {
                CurrentDockingModuleState = DockingModuleStates.Idle;
            }

            // state machine
            public void UpdateDockingModule()
            {
                switch (CurrentDockingModuleState)
                {
                    case DockingModuleStates.Idle:
                        if (DockRequestReceivedFlag)
                        {
                            CR.AddToLogQueue("Docking request received", STULogType.INFO);
                            DockRequestReceivedFlag = false;
                            CurrentDockingModuleState = DockingModuleStates.AuxiliaryHardwareReset;
                        }
                        break;
                    case DockingModuleStates.AuxiliaryHardwareReset:
                        CR.AddToLogQueue("Resetting auxiliary hardware...", STULogType.INFO);

                        CR.GangwayHinge.TargetVelocityRad = Math.Abs(CR.GangwayHinge.TargetVelocityRad) * -1;
                        CR.MainDockConnector.Disconnect();
                        CR.MainDockHinge1.TargetVelocityRad = Math.Abs(CR.MainDockHinge1.TargetVelocityRad) * -1;
                        CR.MainDockHinge2.TargetVelocityRad = Math.Abs(CR.MainDockHinge2.TargetVelocityRad) * -1;
                        CR.MainDockPiston.Velocity = Math.Abs(CR.MainDockPiston.Velocity) * -1;
                        
                        CR.AddToLogQueue($"Hinge 1 position: {CR.MainDockHinge1.Angle}", STULogType.INFO);
                        CR.AddToLogQueue($"Hinge 2 position: {CR.MainDockHinge2.Angle}", STULogType.INFO);
                        CR.AddToLogQueue($"Piston position: {CR.MainDockPiston.CurrentPosition}", STULogType.INFO);
                        CR.AddToLogQueue($"Gangway position: {CR.GangwayHinge.Angle}", STULogType.INFO);
                        CR.AddToLogQueue("");
                        if (Math.Abs(CR.MainDockHinge1.Angle) < 0.1 && Math.Abs(CR.MainDockHinge2.Angle) < 0.1 && CR.MainDockPiston.CurrentPosition < 0.1 && Math.Abs(CR.GangwayHinge.Angle) < 5.1)
                        {
                            CR.AddToLogQueue("Auxiliary hardware reset complete. Ready for docking...", STULogType.INFO);
                            CR.CreateBroadcast("READY", false, STULogType.INFO);
                            CurrentDockingModuleState = DockingModuleStates.Ready;
                        }
                        break;
                    case DockingModuleStates.Ready:
                        if (CR.MergeBlock.IsConnected)
                        {
                            CR.AddToLogQueue("Docking complete", STULogType.OK);
                            CR.CreateBroadcast("Docking complete", false, STULogType.OK);
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
