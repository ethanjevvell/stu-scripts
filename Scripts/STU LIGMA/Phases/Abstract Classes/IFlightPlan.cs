namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public abstract class IFlightPlan {
                public bool IS_FIRST_RUN = true;
                public abstract bool Run();

                // All IFlightPlans should call this method in their Run() method
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
