using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class DockActuatorGroupFSM
        {
            public enum State
            {
                idle,
                activateLight,
                disconnectConnector,
                retractPiston,
                moveHinge1,
                moveHinge2,
                extendPiston,
                connectConnector,
            }

            public State currentState;

            IMyGridProgramRuntimeInfo Runtime;

            DockActuatorGroup HardwareGroup;

            public DockActuatorGroupFSM(DockActuatorGroup hardwareGroup, IMyGridProgramRuntimeInfo runtime)
            {
                HardwareGroup = hardwareGroup;

                currentState = State.idle;
                Runtime = runtime;
            }

            public void Update()
            {
                double timestamp = Runtime.TimeSinceLastRun.TotalMilliseconds;

                switch (currentState)
                {
                    case State.idle:
                        HandleIdle(); break;
                }
            }

            public void HandleIdle()
            {
                if (HardwareGroup.IsStationary())
                {
                    return;
                }
                else
                {
                    currentState = State.activateLight;
                    return;
                }
            }

            public void HandleActivateLight()
            {
                
            }

            public void HandleDisconnectConnector()
            {
                // disconnect connector
                // if actually disconnected, change state, else return
            }

            public void HandleRetractPiston()
            {

            }

            public void HandleMoveHinge1()
            {

            }

            public void HandleMoveHinge2()
            {

            }

            public void HandleExtendPiston()
            {

            }

            public void HandleConnectConnector()
            {

            }
        }
    }
}
