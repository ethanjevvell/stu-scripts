namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public abstract class ILaunchPlan {

                public bool IS_FIRST_RUN = true;
                public abstract bool Run();

                // All ILaunchPlans should call this method in their Run() method
                public virtual void FirstRunTasks() {
                    if (IS_FIRST_RUN) {
                        FlightController.UpdateShipMass();
                        // Disconnect all connectors from launch pad
                        for (var i = 0; i < Connectors.Length; i++) {
                            Connectors[i].Disconnect();
                        }
                        // Disable dampeners to prevent Stuxnet
                        RemoteControl.DampenersOverride = false;
                        foreach (var tank in GasTanks) {
                            tank.Stockpile = false;
                        }
                        IS_FIRST_RUN = false;
                    }
                }

            }
        }
    }
}
