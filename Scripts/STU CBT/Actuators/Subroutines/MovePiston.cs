using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
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
            public class MovePiston : STUStateMachine
            {
                public override string Name => "Move Piston";
                public Sandbox.ModAPI.Ingame.IMyPistonBase RearDockPiston { get; set; }
                public float TargetDistance { get; set; }
                public MovePiston(Sandbox.ModAPI.Ingame.IMyPistonBase piston, float targetDistance)
                {
                    RearDockPiston = piston;
                    TargetDistance = targetDistance;
                }

                public override bool Init()
                {
                    return true;
                }

                public override bool Run()
                {
                    if (Math.Abs(RearDockPiston.CurrentPosition - TargetDistance) < CBTRearDock.PISTON_POSITION_TOLERANCE)
                    {
                        RearDockPiston.Velocity = 0;
                        return true;
                    }
                    else if (RearDockPiston.CurrentPosition < TargetDistance)
                    {
                        RearDockPiston.MaxLimit = TargetDistance;
                        RearDockPiston.Velocity = CBTRearDock.PISTON_TARGET_VELOCITY;
                        return false;
                    }
                    else if (RearDockPiston.CurrentPosition > TargetDistance)
                    {
                        RearDockPiston.MinLimit = TargetDistance;
                        RearDockPiston.Velocity = -CBTRearDock.PISTON_TARGET_VELOCITY;
                        return false;
                    }
                    else return false;
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}
