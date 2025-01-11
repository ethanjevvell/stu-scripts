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
        public class SoloAirlockStateMachine
        {
            public enum State
            {
                Idle,
                Enter,
                Close,
            }
            public State CurrentState { get; set; }
            private IMyDoor Door { get; set; }
            private IMyGridProgramRuntimeInfo Runtime { get; set; }

            public double TimeBufferMS = 1000f;

            public SoloAirlockStateMachine(IMyDoor door, IMyGridProgramRuntimeInfo runtime)
            {
                Door = door;
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
                        if (Door.Status == DoorStatus.Opening) { CurrentState = State.Enter; }
                        Timestamp = CurrentTime;
                        break;
                    case State.Enter:
                        Door.OpenDoor();
                        if (Door.Status == DoorStatus.Open && CurrentTime > Timestamp + TimeBufferMS)
                        {
                            CurrentState = State.Close;
                            Timestamp = CurrentTime;
                        }
                        break;
                    case State.Close:
                        Door.CloseDoor();
                        if (Door.Status == DoorStatus.Closed) { CurrentState = State.Idle; }
                        break;
                }
            }
        }
    }
}
