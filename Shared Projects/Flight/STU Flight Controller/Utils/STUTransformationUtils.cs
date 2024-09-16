using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program {
        internal class STUTransformationUtils {

            /// <summary>
            /// Transforms a world position to a local position relative to a reference block
            /// </summary>
            /// <param name="reference"></param>
            /// <param name="worldDirection"></param>
            /// <returns></returns>
            public static Vector3D WorldDirectionToLocalDirection(IMyTerminalBlock reference, Vector3D worldDirection) {
                // Flip z-axis to undo SE's unintuitive coordinate system, where forward in space is movement in -z
                return Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(reference.WorldMatrix)) * new Vector3D(1, 1, -1);
            }

            /// <summary>
            /// Transforms a local direction to a world direction. IMPORTANT: Assumes you've already flipped the local direction's z-axis. 
            /// </summary>
            /// <param name="reference"></param>
            /// <param name="localDirection"></param>
            /// <returns></returns>
            public static Vector3D LocalDirectionToWorldDirection(IMyTerminalBlock reference, Vector3D localDirection) {
                return Vector3D.TransformNormal(localDirection, reference.WorldMatrix);
                ;
            }

        }
    }
}
