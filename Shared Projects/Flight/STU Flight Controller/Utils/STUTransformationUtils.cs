using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program {
        internal class STUTransformationUtils {

            public static Vector3D LocalDirectionToWorldDirection(IMyTerminalBlock reference, Vector3D localDirection) {
                return Vector3D.TransformNormal(localDirection, MatrixD.Transpose(reference.WorldMatrix));
            }

        }
    }
}
