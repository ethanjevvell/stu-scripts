using Sandbox.ModAPI;
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
            public class MoveHinge : STUStateMachine
            {
                public override string Name => "Move Hinge";
                public IMyMotorStator Hinge { get; set; }
                public float TargetAngle { get; set; }
                public MoveHinge(IMyMotorStator thisHinge, float angle)
                {
                    Hinge = thisHinge;
                    TargetAngle = angle;
                }

                public override bool Init()
                {
                    Hinge.Torque = CBTRearDock.HINGE_TORQUE;
                    return true;
                }

                public override bool Run()
                {
                    return true;
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}
