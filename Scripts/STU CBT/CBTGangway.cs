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
using static IngameScript.Program.CBT;

namespace IngameScript
{
    partial class Program
    {
        public partial class  CBTGangway
        {
            public static IMyMotorStator GangwayHinge1 { get; set; }
            public static IMyMotorStator GangwayHinge2 { get; set; }
            public enum GangwayStates
            {
                Unknown,
                Retracting,
                Retracted,
                Extending,
                Extended
            }
            public static GangwayStates CurrentGangwayState { get; set; } = GangwayStates.Unknown;

            public CBTGangway(IMyMotorStator hinge1, IMyMotorStator hinge2)
            {
                // constructor

                GangwayHinge1 = hinge1;
                GangwayHinge2 = hinge2;
            }

            // main state machine
            public void UpdateGangway()
            {
                switch (CurrentGangwayState)
                {
                    case GangwayStates.Unknown:
                        // do nothing
                        break;
                    case GangwayStates.Retracting:
                        // do nothing
                        break;
                    case GangwayStates.Retracted:
                        // do nothing
                        break;
                    case GangwayStates.Extending:
                        // do nothing
                        break;
                    case GangwayStates.Extended:
                        // do nothing
                        break;
                }
            }

            public bool IsGangwayStateValid()
            { 
                // check whether hinge 1 is out of bounds
                if ((GangwayHinge1.Angle * (180 / Math.PI)) > 0) { return false; }
                // normalize both hinge angles to 0-180 degrees
                float hinge1Angle = (float)(GangwayHinge1.Angle * (180 / Math.PI)) + 90;
                float hinge2Angle = (float)(GangwayHinge2.Angle * (180 / Math.PI)) + 90;
                // test whether hinge2's angle is twice hinge1's angle with a margin of error
                if (Math.Abs(hinge2Angle - (2 * hinge1Angle)) < 2f) { return true; }
                else { return false; }
            }

            public void ResetGangwayActuators()
            {
                GangwayHinge2.TargetVelocityRPM = 0;
                GangwayHinge2.Torque = 0;
                GangwayHinge2.BrakingTorque = 0;

                GangwayHinge1.TargetVelocityRPM = 0;
                GangwayHinge1.Torque = 33000000;
                GangwayHinge1.BrakingTorque = 33000000;

                if ((GangwayHinge1.Angle * (180 / Math.PI)) + 90 > 0)
                {
                    GangwayHinge1.LowerLimitDeg = 0;
                    GangwayHinge1.TargetVelocityRPM = -1;
                }
                else
                {
                    GangwayHinge1.UpperLimitDeg = 0;
                    GangwayHinge1.TargetVelocityRPM = 1;
                }

                GangwayHinge2.Torque = 33000000;
            }

            public void ExtendGangway()
            {
                if (IsGangwayStateValid() && FlightController.GetCurrentSurfaceAltitude() > 10)
                {
                    GangwayHinge1.TargetVelocityRPM = 1;
                    GangwayHinge2.TargetVelocityRPM = 1;
                }
                else { AddToLogQueue("Gangway assy position not valid or altitude too low; manual reset recommended.", STULogType.ERROR); }
            }

            public void RetractGangway()
            {
                if (IsGangwayStateValid() && FlightController.GetCurrentSurfaceAltitude() > 10)
                {
                    GangwayHinge1.TargetVelocityRPM = 1;
                    GangwayHinge2.TargetVelocityRPM = 1;
                }
                else { AddToLogQueue("Gangway assy position not valid or altitude too low; manual reset recommended.", STULogType.ERROR); }
            }

            /// <summary>
            /// "TRUE" to extend, "FALSE" to retract
            /// </summary>
            /// <param name="extend"></param>
            public void ToggleGangway()
            {
                if (CurrentGangwayState == GangwayStates.Retracted) ExtendGangway();
                else if (CurrentGangwayState == GangwayStates.Extended) RetractGangway();
            }
        }
    }
}
