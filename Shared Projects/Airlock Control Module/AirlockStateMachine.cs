using Sandbox.Game.Entities.Cube;
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
        public class AirlockStateMachine
        {
            public enum State
            {
                Idle,
                EnterA,
                EnterB,
                CloseA,
                CloseB,
                OpenA,
                OpenB,
                ExitA,
                ExitB
            }
            public State CurrentState { get; set; }
            private IMyDoor DoorA { get; set; }
            private IMyDoor DoorB { get; set; }
            private IMyGridProgramRuntimeInfo Runtime { get; set; }

            public double TimeBufferMS = 1000f;

            public AirlockStateMachine(IMyDoor doorA, IMyDoor doorB, IMyGridProgramRuntimeInfo runtime)
            {
                DoorA = doorA;
                DoorB = doorB;
                Runtime = runtime;
            }

            private double CurrentTime = 0f;
            private double Timestamp { get; set; }

            public void Update()
            {
                CurrentTime += Runtime.TimeSinceLastRun.TotalMilliseconds;
                switch (CurrentState)
                {
                    case State.Idle:
                        if (DoorA.Status == DoorStatus.Opening) { CurrentState = State.EnterA; }
                        else if (DoorB.Status == DoorStatus.Opening) { CurrentState = State.EnterB; }
                        Timestamp = CurrentTime;
                        break;
                    case State.EnterA:
                        DoorA.OpenDoor();
                        if (DoorA.Status == DoorStatus.Open && CurrentTime > Timestamp + TimeBufferMS)
                        {
                            CurrentState = State.CloseA;
                            Timestamp = CurrentTime;
                        }
                        break;
                    case State.CloseA:
                        DoorA.CloseDoor();
                        if (DoorA.Status == DoorStatus.Closed && CurrentTime > Timestamp + TimeBufferMS)
                        {
                            CurrentState = State.OpenB;
                            Timestamp = CurrentTime;
                        }
                        break;
                    case State.OpenB:
                        DoorB.OpenDoor();
                        if (DoorB.Status == DoorStatus.Open && CurrentTime > Timestamp + TimeBufferMS)
                        {
                            CurrentState = State.ExitB;
                            Timestamp = CurrentTime;
                        }
                        break;
                    case State.ExitB:
                        DoorB.CloseDoor();
                        if (DoorB.Status == DoorStatus.Closed) { CurrentState = State.Idle; }
                        break;
                    case State.EnterB:
                        DoorB.OpenDoor();
                        if (DoorB.Status == DoorStatus.Open && CurrentTime > Timestamp + TimeBufferMS)
                        {
                            CurrentState = State.CloseB;
                            Timestamp = CurrentTime;
                        }
                        break;
                    case State.CloseB:
                        DoorB.CloseDoor();
                        if (DoorB.Status == DoorStatus.Closed && CurrentTime > Timestamp + TimeBufferMS)
                        {
                            CurrentState = State.OpenA;
                            Timestamp = CurrentTime;
                        }
                        break;
                    case State.OpenA:
                        DoorA.OpenDoor();
                        if (DoorA.Status == DoorStatus.Open && CurrentTime > Timestamp + TimeBufferMS)
                        {
                            CurrentState = State.ExitA;
                            Timestamp = CurrentTime;
                        }
                        break;
                    case State.ExitA:
                        DoorA.CloseDoor();
                        if (DoorA.Status == DoorStatus.Closed) { CurrentState = State.Idle; }
                        break;
                }
            }
        }
    }
}
