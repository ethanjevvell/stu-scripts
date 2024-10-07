using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBT
        {
            public class CruisingSpeedManeuver : STUStateMachine
            {
                public override string Name => "Cruising Speed";
                public double InternalForwardVelocity { get; set; }

                public CruisingSpeedManeuver(double forwardVelocity)
                {
                    InternalForwardVelocity = forwardVelocity;
                }

                public override bool Init()
                {
                    // ensure we have access to the thrusters, not gyros, and dampeners are off
                    SetAutopilotControl(true, false, false);
                    return true;
                }

                public override bool Run()
                {
                    bool VzStable = FlightController.SetStableForwardVelocity(InternalForwardVelocity);
                    return VzStable;
                }

                public override bool Closeout()
                {
                    AddToLogQueue("Cruising Speed achieved");
                    return true;
                }
            }
        }
    }
}
