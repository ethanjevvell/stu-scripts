﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class CBTDockingModule
        {
            // variables
            public enum DockingModuleStates
            {
                Idle,
                WaitingForCRReady,
                ConfirmWithPilot,
                Docking,
            }
            public DockingModuleStates CurrentDockingModuleState { get; set; }
            public bool SendDockRequestFlag { get; set; }
            public bool PilotConfirmation { get; set; }
            public Vector3D? DockingPosition { get; set; }

            // constructor
            public CBTDockingModule()
            {
                SendDockRequestFlag = false;
                DockingPosition = null;
                PilotConfirmation = false;
            }

            // state machine
            public void UpdateDockingModule()
            {
                switch (CurrentDockingModuleState)
                {
                    case DockingModuleStates.Idle:
                        DockingPosition = null;
                        if (SendDockRequestFlag)
                        {
                            CBT.AddToLogQueue($"Requesting to dock with the Hyperdrive Ring...", STULogType.INFO);
                            CBT.CreateBroadcast("DOCK", false, STULogType.INFO);
                            SendDockRequestFlag = false;
                            CurrentDockingModuleState = DockingModuleStates.WaitingForCRReady;
                        }
                        break;
                    case DockingModuleStates.WaitingForCRReady:
                        if (DockingPosition != null)
                        {
                            CBT.AddToLogQueue($"Received docking position from the Hyperdrive Ring.", STULogType.INFO);
                            CBT.AddToLogQueue($"Enter \"DOCK\" to proceed.", STULogType.WARNING);
                            CurrentDockingModuleState = DockingModuleStates.ConfirmWithPilot;
                        }
                        break;
                    case DockingModuleStates.ConfirmWithPilot:
                        if (PilotConfirmation)
                        {
                            CBT.AddToLogQueue($"Pilot has confirmed. Initiating docking sequence...", STULogType.INFO);
                            PilotConfirmation = false;
                            CurrentDockingModuleState = DockingModuleStates.Docking;
                        }
                        break;
                    case DockingModuleStates.Docking:
                        break;
                }
            }

        }
    }
    
}
