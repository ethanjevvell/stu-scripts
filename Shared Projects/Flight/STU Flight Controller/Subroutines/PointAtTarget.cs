using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class STUFlightController
        {
            public class PointAtTarget : STUStateMachine
            {
                public override string Name => "Point At Target";

                private STUFlightController FC;
                Vector3D PointToLookAt;

                public PointAtTarget(STUFlightController thisFlightController, Vector3D pointToLookAt)
                {
                    FC = thisFlightController;
                    PointToLookAt = pointToLookAt;
                }

                public override bool Init()
                {
                    FC.ReinstateGyroControl();
                    return true;
                }

                public override bool Run()
                {
                    return FC.AlignShipToTarget(PointToLookAt);
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}