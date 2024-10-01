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
            public class AbortManeuver : STUStateMachine
            {
                public override string Name => "Hover";
                public AbortManeuver(CBT cbt)
                {

                }

                public override bool Init()
                {
                    // ensure we have access to the thrusters, gyros, and dampeners are on
                    SetAutopilotControl(true, true, true);
                    ResetUserInputVelocities();
                    UserInputGangwayState = CBTGangway.GangwayStates.Frozen;
                    UserInputRearDockState = CBTRearDock.RearDockStates.Frozen;

                    return true;
                }

                public override bool Run()
                {
                    bool stableVelocity = FlightController.SetStableForwardVelocity(0);
                    FlightController.SetVr(0);
                    FlightController.SetVp(0);
                    FlightController.SetVw(0);
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
