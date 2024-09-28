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
        public abstract class ManeuverTemplate
        {
            public abstract string Name { get; }

            public enum InternalStates
            {
                Init,
                Run,
                Closeout,
                Done,
            }

            protected virtual InternalStates CurrentInternalState { get; set; } = InternalStates.Init;
            protected abstract bool Init();
            protected abstract void Run();
            protected abstract void Closeout();

            public virtual void Update()
            {
                switch (CurrentInternalState)
                {
                    case InternalStates.Init:
                        if (Init())
                        {
                            CurrentInternalState = InternalStates.Run;
                        }
                        break;
                    case InternalStates.Run:
                        if (Run())
                        {
                            CurrentInternalState = InternalStates.Closeout;
                        }
                        break;
                    case InternalStates.Closeout:
                        if (Closeout())
                        {
                            CurrentInternalState = InternalStates.Done;
                        }
                        break;
                }
            }
        }
    }
}
