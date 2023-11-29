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
                HardwareGroup.IlluminateLight(HardwareGroup.DistanceLights, 1);
                
                currentState = State.disconnectConnector;
                return;
            }

            public void HandleDisconnectConnector()
            {
                // disconnect connector
                HardwareGroup.Disconnect(HardwareGroup.Connector);
                // if actually disconnected, change state, else return

                if (HardwareGroup.Connector.Status != MyShipConnectorStatus.Connected)
                {
                    currentState = State.retractPiston;
                    return;
                }
                else { return; }
            }

            public void HandleRetractPiston()
            {
                HardwareGroup.MovePiston(HardwareGroup.Piston,0);
                if (HardwareGroup.IsStationary()) {
                    currentState = State.moveHinge1;
                    return;
                }
                else { return; }
            }

            public void HandleMoveHinge1()
            {
                HardwareGroup.MoveHinge(HardwareGroup.Hinge1, HardwareGroup.GetTargetPosition()[0]);
                if (HardwareGroup.IsStationary())
                {
                    currentState = State.moveHinge2;
                    return;
                }
                else { return; }
            }

            public void HandleMoveHinge2()
            {
                HardwareGroup.MoveHinge(HardwareGroup.Hinge2, HardwareGroup.GetTargetPosition()[2]);
                if (HardwareGroup.IsStationary())
                {
                    currentState = State.extendPiston;
                    return;
                }
                else { return; }
            }

            public void HandleExtendPiston()
            {
                HardwareGroup.MovePiston(HardwareGroup.Piston, HardwareGroup.GetTargetPosition()[1]);
                if (HardwareGroup.IsStationary())
                {
                    currentState = State.connectConnector;
                    return;
                }
                else { return; }
            }

            public void HandleConnectConnector()
            {
                HardwareGroup.Connect(HardwareGroup.Connector);
                if (HardwareGroup.Connector.Status != MyShipConnectorStatus.Connected)
                {
                    return;
                }
                else
                {
                    currentState = State.idle;
                    return;
                }
            }
        }
    }
}
