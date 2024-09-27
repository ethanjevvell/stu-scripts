namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public abstract class ManeuverTemplate {
                public abstract string Name { get; }

                public enum InternalStates {
                    Init,
                    Run,
                    Closeout,
                    Done,
                }

                protected virtual InternalStates CurrentInternalState { get; set; } = InternalStates.Init;
                protected abstract bool Init();
                protected abstract bool Run();
                protected abstract bool Closeout();

                /// <summary>
                /// Call this method to run the maneuver's state machine
                /// </summary>
                public virtual void RunStateMachine() {
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
                            break;
                    }
                }
            }
        }
    }
}
