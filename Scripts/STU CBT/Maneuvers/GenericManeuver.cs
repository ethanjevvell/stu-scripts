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
            public class GenericManeuver : STUStateMachine
            {
                public override string Name => "Generic";
                public double InternalForwardVelocity { get; set; }
                public double InternalRightVelocity { get; set; }
                public double InternalUpVelocity { get; set; }
                public double InternalRollVelocity { get; set; }
                public double InternalPitchVelocity { get; set; }
                public double InternalYawVelocity { get; set; }

                public GenericManeuver(double forwardVelocity, double rightVelocity, double upVelocity, double rollVelocity, double pitchVelocity, double yawVelocity)
                {
                    InternalForwardVelocity = forwardVelocity;
                    InternalRightVelocity = rightVelocity;
                    InternalUpVelocity = upVelocity;
                    InternalRollVelocity = rollVelocity;
                    InternalPitchVelocity = pitchVelocity;
                    InternalYawVelocity = yawVelocity;
                }
                
                public override bool Init()
                {
                    // ensure we have access to the thrusters, gyros, and dampeners are off
                    SetAutopilotControl(true, true, true);
                    return true;
                }

                public override bool Run()
                {
                    bool VzStable = FlightController.SetVz(InternalForwardVelocity);
                    bool VxStable = FlightController.SetVx(InternalRightVelocity);
                    bool VyStable = FlightController.SetVy(InternalUpVelocity);
                    FlightController.SetVr(InternalRollVelocity * -1); // roll is inverted for some reason and is the only one that works like this on the CBT, not sure about other ships
                    FlightController.SetVp(InternalPitchVelocity);
                    FlightController.SetVw(InternalYawVelocity);

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
