namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public abstract class ITerminalPlan {

                public bool IS_FIRST_RUN = true;
                public abstract int TERMINAL_VELOCITY { get; }

                public abstract bool Run();

                // All ITerminalPlans should call this method in their Run() method
                public virtual void FirstRunTasks() {
                    if (IS_FIRST_RUN) {
                        FlightController.UpdateShipMass();
                        IS_FIRST_RUN = false;
                    }
                }

            }
        }
    }
}

