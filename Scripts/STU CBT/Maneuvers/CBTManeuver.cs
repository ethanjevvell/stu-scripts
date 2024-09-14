using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBT
        {
            public abstract class CBTManeuver
            {
                public abstract string Name { get; }
                public enum InternalStates
                {
                    Init,
                    Run,
                    Closeout,
                    Done,
                }

                public virtual InternalStates CurrentInternalState { get; set; } = InternalStates.Init;
                public abstract bool Init();
                public abstract bool Run();
                public abstract bool Closeout();
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
                        case InternalStates.Done:
                            break;
                    }
                }
            }
        }
    }
}
