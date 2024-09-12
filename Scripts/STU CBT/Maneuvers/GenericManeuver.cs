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
            public class GenericManeuver : CBTManeuver
            {
                public override bool Init()
                {
                    // ensure we have access to the thrusters, gyros, and dampeners are off
                    FlightController.ReinstateThrusterControl();
                    FlightController.ReinstateGyroControl();
                    RemoteControl.DampenersOverride = false;
                    
                    return FlightController.HasThrusterControl && FlightController.HasGyroControl;
                }

                public override bool Run()
                {
                    FlightController.ReinstateGyroControl();
                    FlightController.ReinstateThrusterControl();
                    bool VzStable = FlightController.SetVz(UserInputForwardVelocity);
                    bool VxStable = FlightController.SetVx(UserInputRightVelocity);
                    bool VyStable = FlightController.SetVy(UserInputUpVelocity);
                    FlightController.SetVr(UserInputRollVelocity * -1); // roll is inverted for some reason and is the only one that works like this on the CBT, not sure about other ships
                    FlightController.SetVp(UserInputPitchVelocity);
                    FlightController.SetVw(UserInputYawVelocity);
                    return VxStable && VzStable && VyStable;
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}
