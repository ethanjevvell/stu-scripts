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
                Vector3D ReferenceFace;

                public PointAtTarget(STUFlightController thisFlightController, Vector3D pointToLookAt, IMyTerminalBlock reference = null, Vector3D? referenceFace = null)
                {
                    FC = thisFlightController;
                    PointToLookAt = pointToLookAt;
                    ReferenceBlock = reference;
                    ReferenceFace = referenceFace.Value == null ? Vector3D.Zero : referenceFace.Value;
                }

                public override bool Init()
                {
                    FC.ReinstateGyroControl();
                    return true;
                }

                public override bool Run()
                {
                    return FC.AlignShipToTarget(PointToLookAt, ReferenceFace);
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}