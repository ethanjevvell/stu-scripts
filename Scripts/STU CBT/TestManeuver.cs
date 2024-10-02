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
            public class TestManeuver : STUStateMachine
            {
                public override string Name => "Test";
                public Vector3D PointToLookAt { get; set; }

                public TestManeuver(Vector3D pointToLookAt)
                {
                    PointToLookAt = pointToLookAt;
                }

                public override bool Init()
                {
                    // ensure we have access to the thrusters, gyros, and dampeners are off
                    SetAutopilotControl(true, true, false);
                    return true;
                }

                public override bool Run()
                {
                    return FlightController.AlignShipToTarget(PointToLookAt);
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}
