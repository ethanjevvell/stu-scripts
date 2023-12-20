namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public abstract class ILaunchPlan {

                public bool IS_FIRST_RUN = true;
                public abstract bool Run();

                // All ILaunchPlans should call this method in their Run() method
                public virtual void FirstRunTasks() {
                    if (IS_FIRST_RUN) {
                        // Disconnect from launch pad connector
                        Connector.Disconnect();
                        // Disable dampeners to prevent Stuxnet
                        RemoteControl.DampenersOverride = false;
                        IS_FIRST_RUN = false;
                    }
                }
            }
        }
    }
}
