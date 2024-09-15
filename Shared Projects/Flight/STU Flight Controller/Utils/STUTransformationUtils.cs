using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program {
        internal class STUTransformationUtils {

            public static Vector3D LocalDirectionToWorldDirection(IMyTerminalBlock reference, Vector3D localDirection) {
                // Flip z-axis to undo SE's unintuitive coordinate system, where forward in space is movement in -z
                return Vector3D.TransformNormal(localDirection, MatrixD.Transpose(reference.WorldMatrix)) * new Vector3D(1, 1, -1);
            }

        }
    }
}
