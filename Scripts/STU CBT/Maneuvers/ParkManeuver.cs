using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBT
        {
            public class ParkManeuver : STUStateMachine
            {
                public override string Name => "Park";
                Queue<STUStateMachine> ManeuverQueue;
                Vector3D CRMergeBlock_position;
                Vector3D CRMergeBlock_orientation;
                Vector3D StagingPosition;
                public ParkManeuver(Queue<STUStateMachine> thisManeuverQueue, Vector3D _CRMergeBlock_position, Vector3D _CRMergeBlock_orientation)
                {
                    ManeuverQueue = thisManeuverQueue;
                    CRMergeBlock_position = _CRMergeBlock_position;
                    CRMergeBlock_orientation = _CRMergeBlock_orientation;
                }

                public override bool Init()
                {
                    // ensure we have access to the thrusters, gyros, and dampeners are on
                    SetAutopilotControl(true, true, true);
                    ResetUserInputVelocities();
                    StagingPosition = CalculateStagingPosition(CRMergeBlock_position);
                    return true;
                }

                public override bool Run()
                {
                    ManeuverQueue.Enqueue(new STUFlightController.GotoAndStop(FlightController, StagingPosition, 5));
                    ManeuverQueue.Enqueue(new STUFlightController.PointAtTarget(FlightController, CRMergeBlock_orientation));
                    ManeuverQueue.Enqueue(new ExtendGangwayManeuver());
                    ManeuverQueue.Enqueue(new STUFlightController.GotoAndStop(FlightController, CRMergeBlock_position, 5));
                    return true;
                }

                public override bool Closeout()
                {
                    return true;
                }

                public Vector3D CalculateStagingPosition(Vector3D mergeBlockPosition)
                {
                    Vector3D stagingPosition = mergeBlockPosition;
                    return stagingPosition;
                }
            }
        }
    }
}
