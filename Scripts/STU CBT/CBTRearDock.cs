using Sandbox.Game.Screens.DebugScreens;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBTRearDock
        {
            // variables
            public IMyPistonBase RearDockPiston { get; set; }
            public IMyMotorStator RearDockHinge1 { get; set; }
            public IMyMotorStator RearDockHinge2 { get; set; }
            public IMyShipConnector RearDockConnector { get; set; }

            public enum RearDockActuatorsState
            {
                Unknown,
                Retracted,
                Retracting,
                Extended,
                Extending,
            }
            public static RearDockActuatorsState CurrentRearDockActuatorsState = RearDockActuatorsState.Unknown;

            public CBTRearDock(IMyPistonBase piston, IMyMotorStator hinge1, IMyMotorStator hinge2, IMyShipConnector connector)
            {
                // constructor

                RearDockPiston = piston;
                RearDockHinge1 = hinge1;
                RearDockHinge2 = hinge2;
                RearDockConnector = connector;
            }
        }
    }
}
