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
                string ReferenceFace;

                public PointAtTarget(STUFlightController thisFlightController, Vector3D pointToLookAt, IMyTerminalBlock referenceBlock = null, string referenceFace = null)
                {
                    FC = thisFlightController;
                    PointToLookAt = pointToLookAt;
                    ReferenceBlock = referenceBlock;
                    ReferenceFace = referenceFace;
                }

                public override bool Init()
                {
                    FC.ReinstateGyroControl();
                    return true;
                }

                public override bool Run()
                {
                    return FC.AlignShipToTarget(PointToLookAt, ReferenceBlock, ReferenceFace);
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}