using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        public class AirlockFSM
        {
            public STUMasterLogBroadcaster LogBroadcaster;
            
            public enum State
            {
                idle,
                entryToShip,
                entryToStation,
                closeAToShip,
                closeAToStation,
                openBToShip,
                openBToStation,
                exitToShip,
                exitToStation,
                closeBToShip,
                closeBToStation
            }

            public State currentState;

            public List<IMyTerminalBlock> listDockAirlocks;

            public IMyDoor DoorA;
            public IMyDoor DoorB;

            double timeSinceLastRun = 0;

            Action<string> Echo;
            IMyGridProgramRuntimeInfo Runtime;

            int playerCharacterTransitionDelay_MS = 1000;


            public AirlockFSM(IMyDoor doorA, IMyDoor doorB, Action<string> echo, IMyGridProgramRuntimeInfo runtime, IMyIntergridCommunicationSystem IGC) {
                DoorA = doorA;
                DoorB = doorB;
                currentState = State.idle;
                Echo = echo;
                Runtime = runtime;
                LogBroadcaster = new STUMasterLogBroadcaster("LHQ_MASTER_LOGGER", IGC, TransmissionDistance.CurrentConstruct);
            }

            public void Update()
            {
                double timestamp = Runtime.TimeSinceLastRun.TotalMilliseconds;
                

                switch (currentState)
                {
                    case State.idle:
                        HandleIdle(); break;

                    case State.entryToShip:
                        LogBroadcaster.Log(new STULog
                        {
                            Sender = "Airlock Controller",
                            Message = $"Airlock {DoorA.CustomName} opened",
                            Type = STULogType.OK
                        });
                        HandleEntryToShip(); break;

                    case State.entryToStation:
                        LogBroadcaster.Log(new STULog
                        {
                            Sender = "Airlock Controller",
                            Message = $"Airlock {DoorB.CustomName} opened",
                            Type = STULogType.OK
                        });
                        HandleEntryToStation(); break;

                    case State.closeAToShip:
                        HandleCloseAToShip(); break;

                    case State.closeAToStation:
                        HandleCloseAToStation(); break;

                    case State.openBToShip:
                        HandleOpenBToShip(); break;

                    case State.openBToStation:
                        HandleOpenBToStation(); break;

                    case State.exitToShip:
                        HandleExitToShip(); break;

                    case State.exitToStation:
                        HandleExitToStation(); break;

                    case State.closeBToShip:
                        HandleCloseBToShip(); break;

                    case State.closeBToStation:
                        HandleCloseBToStation(); break;
                }
            }

            public void HandleIdle()
            {
                if (DoorA.Status == DoorStatus.Closed && DoorB.Status == DoorStatus.Closed)
                {
                    currentState = State.idle;
                    return;
                }

                if (DoorA.Status == DoorStatus.Open)
                {
                    currentState = State.entryToShip;
                    return;
                }

                if (DoorB.Status == DoorStatus.Open)
                {
                    currentState = State.entryToStation;
                    return;
                }
            }


            public void HandleEntryToShip()
            {
                timeSinceLastRun += Runtime.TimeSinceLastRun.TotalMilliseconds;

                if (timeSinceLastRun > playerCharacterTransitionDelay_MS)
                {
                    currentState = State.closeAToShip;
                    return;
                }
            }

            public void HandleEntryToStation()
            {
                timeSinceLastRun += Runtime.TimeSinceLastRun.TotalMilliseconds;

                if (timeSinceLastRun > playerCharacterTransitionDelay_MS)
                {
                    currentState = State.closeAToStation;
                    return;
                }
            }

            public void HandleCloseAToShip()
            {
                DoorA.CloseDoor();

                if (DoorA.Status != DoorStatus.Closed)
                {
                    return;
                }
                else
                {
                    currentState = State.openBToShip;
                    return;
                }

            }

            public void HandleCloseAToStation()
            {
                DoorB.CloseDoor();

                if (DoorB.Status != DoorStatus.Closed)
                {
                    return;
                }
                else
                {
                    currentState = State.openBToStation;
                    return;
                }

            }

            public void HandleOpenBToShip()
            {
                DoorB.OpenDoor();

                if (DoorB.Status != DoorStatus.Open)
                {
                    return;
                }
                else
                {
                    currentState = State.exitToShip;
                    return;
                }

            }

            public void HandleOpenBToStation()
            {
                DoorA.OpenDoor();

                if (DoorA.Status != DoorStatus.Open)
                {
                    return;
                }
                else
                {
                    currentState = State.exitToStation;
                    return;
                }

            }

            public void HandleExitToShip()
            {
                timeSinceLastRun += Runtime.TimeSinceLastRun.TotalMilliseconds;

                if (timeSinceLastRun > playerCharacterTransitionDelay_MS)
                {
                    currentState = State.closeBToShip;
                    return;
                }
            }

            public void HandleExitToStation()
            {
                timeSinceLastRun += Runtime.TimeSinceLastRun.TotalMilliseconds;

                if (timeSinceLastRun > playerCharacterTransitionDelay_MS + 1500)
                {
                    currentState = State.closeBToStation;
                    return;
                }
            }

            public void HandleCloseBToShip()
            {
                DoorB.CloseDoor();

                if (DoorB.Status != DoorStatus.Closed)
                {
                    return;
                }
                else
                {
                    currentState = State.idle;
                    return;
                }

            }

            public void HandleCloseBToStation()
            {
                DoorA.CloseDoor();

                if (DoorA.Status != DoorStatus.Closed)
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
