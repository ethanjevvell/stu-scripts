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
            public class FastStopManeuver : STUStateMachine
            {
                public override string Name => "Fast Stop";
                public FastStopManeuver(CBT cbt)
                {

                }

                public override bool Init()
                {
                    AddToLogQueue("FAST STOP NOT IMPLEMENTED", STULogType.WARNING);
                    return true;
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
