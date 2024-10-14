
namespace IngameScript {
    partial class Program {

        public abstract class STUStateMachine {

            public abstract string Name { get; }

            public enum InternalStates {
                Init,
                Run,
                Closeout,
                Done
            }

            public virtual InternalStates CurrentInternalState { get; set; } = InternalStates.Init;
            public abstract bool Init();
            public abstract bool Run();
            public abstract bool Closeout();

            /// <summary>
            /// Call this method to run the maneuver's state machine
            /// </summary>
            public virtual bool ExecuteStateMachine() {
                switch (CurrentInternalState) {
                    case InternalStates.Init:
                        if (Init()) {
                            CurrentInternalState = InternalStates.Run;
                        }
                        break;
                    case InternalStates.Run:
                        if (Run()) {
                            CurrentInternalState = InternalStates.Closeout;
                        }
                        break;
                    case InternalStates.Closeout:
                        if (Closeout()) {
                            CurrentInternalState = InternalStates.Done;
                        }
                        break;
                    case InternalStates.Done:
                        return true;

                }
                return false;
            }
        }
    }
}
