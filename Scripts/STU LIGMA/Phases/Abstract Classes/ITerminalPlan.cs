namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public abstract class ITerminalPlan {
                public bool IS_FIRST_RUN = true;
                public abstract bool Run();

                // All ITerminalPlans should call this method in their Run() method
                public virtual void FirstRunTasks() {
                    if (IS_FIRST_RUN) {
                        FlightController.UpdateShipMass();
                        CreateErrorBroadcast(LIGMA_VARIABLES.COMMANDS.SendGoodbye);
                        IS_FIRST_RUN = false;
                    }
                }
            }
        }
    }
}

