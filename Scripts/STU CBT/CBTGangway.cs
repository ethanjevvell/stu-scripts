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
// using static IngameScript.Program.CBT;

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
                Extended, 
                Resetting0,
            }
            public static GangwayStates CurrentGangwayState { get; set; } = GangwayStates.Unknown;

            public CBTGangway(IMyMotorStator hinge1, IMyMotorStator hinge2)
            {
                // constructor

                GangwayHinge1 = hinge1;
                GangwayHinge2 = hinge2;
                CurrentGangwayState = GangwayStates.Unknown;
            }

            // main state machine
            public void UpdateGangway()
            {
                switch (CurrentGangwayState)
                {
                    case GangwayStates.Unknown:
                        if (IsGangwayStateValid())
                        {
                            CurrentGangwayState = GangwayStates.Extended;
                        }
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
                if (Math.Abs(GangwayHinge1.Angle * (180 / Math.PI)) > 0.1) { return false; }
                // normalize both hinge angles to 0-180 degrees
                float hinge1Angle = (float)(GangwayHinge1.Angle * (180 / Math.PI)) + 90;
                float hinge2Angle = (float)(GangwayHinge2.Angle * (180 / Math.PI)) + 90;
                // test whether hinge2's angle is twice hinge1's angle with a margin of error
                if (Math.Abs(hinge2Angle - (2 * hinge1Angle)) < 2f) { 
                    if ( Math.Abs(hinge1Angle) < 0.5 && Math.Abs(hinge2Angle - 90) < 0.5)
                    {
                        CurrentGangwayState = GangwayStates.Extended;
                    }
                    return true; // just because this function returns true, does not mean that the state has been determined. 
                    // all this block does is determine whether hinge 2's angle is double hinge 1's angle. 
                    // now, with that being said, it's probably a good idea to go ahead and call Reset() or whatever, which we will use to actually determine what state the gangway is in.

                }
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
                if (IsGangwayStateValid() ) // && CBT.FlightController.GetCurrentSurfaceAltitude() > 10)
                {
                    GangwayHinge1.TargetVelocityRPM = 1;
                    GangwayHinge2.TargetVelocityRPM = 1;

                    // CurrentGangwayState = GangwayStates.Extending;

                    CurrentGangwayState = GangwayStates.Extended;
                }
                else { CBT.AddToLogQueue("Gangway assy position invalid or altitude too low; manual reset recommended.", STULogType.ERROR); }
            }

            public void RetractGangway()
            {
                if (true) // (IsGangwayStateValid() ) // && CBT.FlightController.GetCurrentSurfaceAltitude() > 10)
                {
                    GangwayHinge1.TargetVelocityRPM = -1;
                    GangwayHinge2.TargetVelocityRPM = -1;

                    // CurrentGangwayState = GangwayStates.Retracting;

                    CurrentGangwayState = GangwayStates.Retracted;
                }
                else { CBT.AddToLogQueue("Gangway assy position invalid or altitude too low; manual reset recommended.", STULogType.ERROR); }
            }

            /// <summary>
            /// "TRUE" to extend, "FALSE" to retract
            /// </summary>
            /// <param name="extend"></param>
            public void ToggleGangway()
            {
                CBT.AddToLogQueue($"Current Gangway State: {CurrentGangwayState}", STULogType.INFO);
                if (CurrentGangwayState == GangwayStates.Retracted) ExtendGangway();
                else if (CurrentGangwayState == GangwayStates.Extended) RetractGangway();
            }
        }
    }
}
