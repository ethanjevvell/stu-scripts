namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public abstract class IDescentPlan {
                public bool IS_FIRST_RUN = true;
                public abstract bool Run();

                // All ILaunchPlans should call this method in their Run() method
                public virtual void FirstRunTasks() {
                    if (IS_FIRST_RUN) {
                        FlightController.UpdateShipMass();
                        // Detonation sensor activated for all descent plans
                        DetonationSensor.Enabled = true;
                        ArmWarheads();
                        IS_FIRST_RUN = false;
                    }
                }
            }
        }
    }
}

