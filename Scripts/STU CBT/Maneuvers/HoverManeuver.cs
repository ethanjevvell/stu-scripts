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
            public class HoverManeuver : CBTManeuver
            {
                public override string Name => "Hover";
                public HoverManeuver(CBT cbt)
                {
                    
                }
                
                public override bool Init()
                {
                    // ensure we have access to the thrusters, gyros, and dampeners are off
                    SetAutopilotControl(true, true, true);
                    bool stableVelocity = FlightController.SetStableForwardVelocity(0);
                    FlightController.SetVr(0);
                    FlightController.SetVp(0);
                    FlightController.SetVw(0);

                    return stableVelocity;
                }

                public override bool Run()
                {
                    return true;
                }

                public override bool Closeout()
                {
                    ResetUserInputVelocities();
                    return true;
                }
            }
        }
    }
}
