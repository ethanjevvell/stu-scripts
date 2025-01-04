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

            public AirlockStateMachine(IMyDoor doorA, IMyDoor doorB)
            {
                DoorA = doorA;
                DoorB = doorB;
            }

            public void Update()
            {
                switch (CurrentState)
                {
                    case State.Idle:
                        if (DoorA.Status == DoorStatus.Opening) { CurrentState = State.EnterA; }
                        else if (DoorB.Status == DoorStatus.Opening) { CurrentState = State.EnterB; }
                        break;
                    case State.EnterA:
                        DoorA.OpenDoor();
                        if (DoorA.Status == DoorStatus.Open) { CurrentState = State.CloseA; }
                        break;
                    case State.CloseA:
                        DoorA.CloseDoor();
                        if (DoorA.Status == DoorStatus.Closed) { CurrentState = State.OpenB; }
                        break;
                    case State.OpenB:
                        DoorB.OpenDoor();
                        if (DoorB.Status == DoorStatus.Open) { CurrentState = State.ExitB; }
                        break;
                    case State.ExitB:
                        DoorB.CloseDoor();
                        if (DoorB.Status == DoorStatus.Closed) { CurrentState = State.Idle; }
                        break;
                    case State.EnterB:
                        DoorB.OpenDoor();
                        if (DoorB.Status == DoorStatus.Open) { CurrentState = State.CloseB; }
                        break;
                    case State.CloseB:
                        DoorB.CloseDoor();
                        if (DoorB.Status == DoorStatus.Closed) { CurrentState = State.OpenA; }
                        break;
                    case State.OpenA:
                        DoorA.OpenDoor();
                        if (DoorA.Status == DoorStatus.Open) { CurrentState = State.ExitA; }
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
