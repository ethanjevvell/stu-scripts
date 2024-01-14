namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class SpaceToSpaceDescentPlan : IDescentPlan {
                public override bool Run() {
                    // Do nothing; there is no "descent" in space.
                    FirstRunTasks();
                    return true;
                }
            }
        }
    }
}