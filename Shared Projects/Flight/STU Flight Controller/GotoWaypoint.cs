namespace IngameScript
{
    partial class Program
    {
        public partial class STUFlightController
        {
            public class GotoWaypoint : STUStateMachine
            {
                public override string Name => "Point At Target";

                private double oneTickAcceleration;
                private STUFlightController FC;

                public GotoWaypoint(STUFlightController thisFlightController)
                {
                    oneTickAcceleration = 0;
                    FC = thisFlightController;
                }

                public override bool Init()
                {
                    return true;
                }

                public override bool Run()
                {
                    return true;
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}