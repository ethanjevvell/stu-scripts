using Sandbox.ModAPI.Ingame;
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
                IMyTerminalBlock ReferenceBlock;

                public PointAtTarget(STUFlightController thisFlightController, Vector3D pointToLookAt, IMyTerminalBlock reference = null)
                {
                    FC = thisFlightController;
                    PointToLookAt = pointToLookAt;
                    ReferenceBlock = reference;
                }

                public override bool Init()
                {
                    FC.ReinstateGyroControl();
                    return true;
                }

                public override bool Run()
                {
                    return FC.AlignShipToTarget(PointToLookAt, ReferenceBlock);
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}