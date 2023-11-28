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
        public class DockActuatorFSM
        {
            public STUMasterLogBroadcaster LogBroadcaster;

            public enum State
            {
                idle,
                pistonMoving,
                hinge1moving,
                hinge2moving,
            }

            public State currentState;

            public IMyMotorStator Hinge1;
            public IMyPistonBase Piston;
            public IMyMotorStator Hinge2;

            double timeSinceLastRun = 0;

            Action<string> Echo;
            IMyGridProgramRuntimeInfo Runtime;

            int safetyDelayFactor_MS = 2000;

            public DockActuatorFSM(IMyMotorStator hinge1, IMyPistonBase piston, IMyMotorStator hinge2, Action<string> echo, IMyGridProgramRuntimeInfo runtime)
            {
                Hinge1 = hinge1;
                Piston = piston;
                Hinge2 = hinge2;
                Echo = echo;
                Runtime = runtime;
            }

            public void Update()
            {
                double timestamp = Runtime.TimeSinceLastRun.TotalMilliseconds;

                switch (currentState)
                {
                    case State.idle:
                        HandleIdle(); break;

                    case State.pistonMoving:
                        HandlePistonMoving(); break;

                    case State.hinge1moving:
                        HandleHinge1Moving(); break;

                    case State.hinge2moving:
                        HandleHinge2Moving(); break;
                }
            }

            public void HandleIdle()
            {

            }

            public void HandlePistonMoving()
            {

            }

            public void HandleHinge1Moving()
            {

            }

            public void HandleHinge2Moving()
            {

            }
        }
    }
}
