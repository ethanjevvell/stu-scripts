﻿using Sandbox.Game.Screens.DebugScreens;
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
            private const float HINGE_ANGLE_TOLERANCE = 0.1f;
            public static IMyMotorStator GangwayHinge1 { get; set; }
            public static IMyMotorStator GangwayHinge2 { get; set; }
            public enum GangwayStates
            {
                Unknown,
                Retracting,
                Retracted,
                Extending,
                Extended, 
                Resetting,
                ResettingHinge1,
                ResettingHinge2,
                Frozen,
            }
            public GangwayStates CurrentGangwayState { get; set; }
            private GangwayStates LastUserInputGangwayState { get; set; }

            public CBTGangway(IMyMotorStator hinge1, IMyMotorStator hinge2)
            {
                // constructor

                GangwayHinge1 = hinge1;
                GangwayHinge2 = hinge2;
                CurrentGangwayState = GangwayStates.Unknown;
                LastUserInputGangwayState = CBT.UserInputGangwayState;
            }

            // main state machine
            public void UpdateGangway()
            {
                if (CBT.UserInputGangwayState != CurrentGangwayState && CBT.UserInputGangwayState != LastUserInputGangwayState)
                {
                    if (CanGoToRequestedState(CBT.UserInputGangwayState))
                    {
                        CBT.AddToLogQueue($"Changing internal gangway state to user input: {CBT.UserInputGangwayState}", STULogType.INFO);
                        CurrentGangwayState = CBT.UserInputGangwayState;
                        LastUserInputGangwayState = CBT.UserInputGangwayState;
                    }
                    else
                    {
                        CBT.AddToLogQueue($"Cannot go to requested state {CBT.UserInputGangwayState} cause of da rulez", STULogType.ERROR);
                    }
                }
                switch (CurrentGangwayState)
                {
                    case GangwayStates.Unknown:
                        // initialization state, do nothing
                        break;

                    case GangwayStates.Resetting:
                        if (ResetGangwayActuators()) { CurrentGangwayState = GangwayStates.ResettingHinge2; }
                        break;

                    case GangwayStates.Retracting:
                        GangwayHinge1.RotorLock = false;
                        GangwayHinge2.RotorLock = false;
                        if (RetractGangway()) { CurrentGangwayState = GangwayStates.Retracted; }
                        break;

                    case GangwayStates.Retracted:
                        // DO NOTHING
                        break;

                    case GangwayStates.Extending:
                        GangwayHinge1.RotorLock = false;
                        GangwayHinge2.RotorLock = false;
                        if (ExtendGangway()) { CurrentGangwayState = GangwayStates.Extended; }
                        break;

                    case GangwayStates.Extended:
                        // DO NOTHING
                        break;

                    case GangwayStates.ResettingHinge1:
                        if (ResetHinge1()) { CurrentGangwayState = GangwayStates.Extended;}
                        break;

                    case GangwayStates.ResettingHinge2:
                        if (ResetHinge2()) { CurrentGangwayState = GangwayStates.ResettingHinge1; }
                        break;

                    case GangwayStates.Frozen:
                        CBT.AddToLogQueue($"Halting Gangway Velocities.", STULogType.INFO);
                        GangwayHinge1.TargetVelocityRad = 0;
                        GangwayHinge2.TargetVelocityRad = 0;
                        break;
                }
            }

            public bool CanGoToRequestedState(GangwayStates requestedState)
            {
                CBT.AddToLogQueue($"asking the questioon about {requestedState}", STULogType.INFO);
                switch (requestedState)
                {
                    // (case) HowDoIGetToThisState?
                        // (return) you can only get to this state if these || conditions !& are && met
                        // i.e. you can get to Unknown from anywhere, but you can only get to ResettingHinge1 from ResettingHinge2
                    case GangwayStates.Unknown:
                        return true;
                    case GangwayStates.Resetting:
                        return true;
                    case GangwayStates.Retracting:
                        return CurrentGangwayState == GangwayStates.Extended || CurrentGangwayState == GangwayStates.Retracted;
                    case GangwayStates.Retracted:
                        return CurrentGangwayState == GangwayStates.Retracting;
                    case GangwayStates.Extending:
                        return CurrentGangwayState == GangwayStates.Retracted || CurrentGangwayState == GangwayStates.Extended;
                    case GangwayStates.Extended:
                        return CurrentGangwayState == GangwayStates.Extending;
                    case GangwayStates.ResettingHinge1:
                        return CurrentGangwayState == GangwayStates.ResettingHinge2;
                    case GangwayStates.ResettingHinge2:
                        return CurrentGangwayState == GangwayStates.Resetting;
                    case GangwayStates.Frozen:
                        return true;
                    default:
                        return false;
                }
            }

            public bool IsGangwayStateValid()
            { 
                // check whether hinge 1 is out of bounds (should be 0 degrees)
                if (Math.Abs(GangwayHinge1.Angle * (180 / Math.PI)) > HINGE_ANGLE_TOLERANCE) { return false; }
                // normalize both hinge angles to 0-180 degrees
                float hinge1Angle = (float)(GangwayHinge1.Angle * (180 / Math.PI)) + 90;
                float hinge2Angle = (float)(GangwayHinge2.Angle * (180 / Math.PI)) + 90;
                // test whether hinge2's angle is twice hinge1's angle with a margin of error
                if (Math.Abs(hinge2Angle - (2 * hinge1Angle)) < 2f) { 
                    //if ( Math.Abs(hinge1Angle) < 0.5 && Math.Abs(hinge2Angle - 90) < 0.5)
                    //{
                    //    CurrentGangwayState = GangwayStates.Extended;
                    //}
                    return true; 
                    // just because this function returns true, does not mean that the state has been determined. 
                    // all this block does is determine whether hinge 2's angle is double hinge 1's angle. 
                    // now, with that being said, it's probably a good idea to call Reset(), which will end up extending the gangway.

                }
                else { return false; }
            }

            private bool ResetGangwayActuators()
            {
                GangwayHinge2.TargetVelocityRPM = 0;
                GangwayHinge2.Torque = 0;
                GangwayHinge2.BrakingTorque = 0;
                GangwayHinge2.RotorLock = false;
                GangwayHinge2.UpperLimitDeg = 90;
                GangwayHinge2.LowerLimitDeg = -90;

                GangwayHinge1.TargetVelocityRPM = 0;
                GangwayHinge1.Torque = 0;
                GangwayHinge1.BrakingTorque = 0;
                GangwayHinge1.RotorLock = false;
                GangwayHinge1.UpperLimitDeg = 90;
                GangwayHinge1.LowerLimitDeg = -90;

                return true;
            }

            private bool ResetHinge1() // hinge 1 should be reset AFTER hinge 2
            {
                GangwayHinge1.RotorLock = false;
                GangwayHinge1.Torque = 33000000;
                if (GangwayHinge1.Angle <= HINGE_ANGLE_TOLERANCE) // if it's close to 0 degrees, stop it, lock it, and set limits
                {
                    CBT.AddToLogQueue($"Hinge 1 is close to 0 degrees", STULogType.INFO);
                    GangwayHinge1.TargetVelocityRPM = 0;
                    GangwayHinge1.UpperLimitDeg = 0;
                    GangwayHinge1.LowerLimitDeg = -90;
                    GangwayHinge1.BrakingTorque = 33000000;
                    GangwayHinge1.RotorLock = true;
                    return true;
                }
                else if (HINGE_ANGLE_TOLERANCE - GangwayHinge1.Angle < 0) // see whether the hinge was in a positive-angle state (which it shouldn't be in)
                {
                    GangwayHinge1.TargetVelocityRPM = -0.7f; // if it was in a positive-angle state, put velocity negative to bring it to zero
                    return false;
                }
                else if (HINGE_ANGLE_TOLERANCE - GangwayHinge1.Angle > 0)
                {
                    GangwayHinge1.TargetVelocityRPM = 0.7f; // if it was in a negative-angle state, put velocity positive to bring it to zero
                    return false;
                }
                else { return false; }
            }

            private bool ResetHinge2() // this one is actually ran first
            {
                GangwayHinge2.RotorLock = false;
                GangwayHinge2.TargetVelocityRPM = 1;
                GangwayHinge2.Torque = 33000000;
                if (Math.Abs(GangwayHinge2.Angle - (Math.PI/2)) < HINGE_ANGLE_TOLERANCE)
                {
                    CBT.AddToLogQueue($"Hinge 2 is close to 90 degrees", STULogType.INFO);
                    GangwayHinge2.TargetVelocityRPM = 0;
                    GangwayHinge2.UpperLimitDeg = 90;
                    GangwayHinge2.LowerLimitDeg = -90;
                    GangwayHinge2.BrakingTorque = 33000000;
                    GangwayHinge2.RotorLock = true;
                    return true;
                }
                else { return false; }
            }

            private bool ExtendGangway()
            {
                if (IsGangwayStateValid())
                {
                    GangwayHinge1.TargetVelocityRPM = 1;
                    GangwayHinge2.TargetVelocityRPM = 1;
                    if (Math.Abs(GangwayHinge1.Angle * (180 / Math.PI) + 90) < HINGE_ANGLE_TOLERANCE && Math.Abs(GangwayHinge2.Angle * (180 / Math.PI) + 90) - 90 < HINGE_ANGLE_TOLERANCE) // is hinge1 close enough to 0, and hinge2 close enough to +90?
                    {
                        GangwayHinge1.TargetVelocityRPM = 0;
                        GangwayHinge2.TargetVelocityRPM = 0;
                        CBT.AddToLogQueue("Gangway Extended.", STULogType.OK);
                        return true;
                    }
                    else { return false; }
                }
                else {
                    CBT.AddToLogQueue("Gangway assy position invalid:", STULogType.WARNING);
                    CBT.AddToLogQueue($"Hinge 1: {GangwayHinge1.Angle}", STULogType.WARNING);
                    CBT.AddToLogQueue($"Hinge 2: {GangwayHinge2.Angle}", STULogType.WARNING);
                    CBT.AddToLogQueue("Manual Reset Recommended", STULogType.ERROR);
                    return false;
                }
            }

            private bool RetractGangway()
            {
                if (IsGangwayStateValid())
                {
                    GangwayHinge1.TargetVelocityRPM = -1;
                    GangwayHinge2.TargetVelocityRPM = -1;
                    if (Math.Abs(GangwayHinge1.Angle * (180 / Math.PI) + 90) < HINGE_ANGLE_TOLERANCE && Math.Abs(GangwayHinge2.Angle * (180 / Math.PI) + 90) - 90 < HINGE_ANGLE_TOLERANCE) // is hinge1 close enough to 0, and hinge2 close enough to +90?
                    {
                        GangwayHinge1.TargetVelocityRPM = 0;
                        GangwayHinge2.TargetVelocityRPM = 0;
                        CBT.AddToLogQueue("Gangway Retracted.", STULogType.OK);
                        return true;
                    }
                    else { return false; }
                }
                else
                {
                    CBT.AddToLogQueue("Gangway assy position invalid:", STULogType.WARNING);
                    CBT.AddToLogQueue($"Hinge 1: {GangwayHinge1.Angle}", STULogType.WARNING);
                    CBT.AddToLogQueue($"Hinge 2: {GangwayHinge2.Angle}", STULogType.WARNING);
                    CBT.AddToLogQueue("Manual Reset Recommended", STULogType.ERROR);
                    return false;
                }
            }

            /// <summary>
            /// "TRUE" to extend, "FALSE" to retract
            /// </summary>
            /// <param name="extend"></param>
            public void ToggleGangway()
            {
                if (CurrentGangwayState == GangwayStates.Unknown) {
                    CBT.AddToLogQueue($"Gangway state unknown; to automatically reset, enter 'gangwayreset' in the prompt.", STULogType.WARNING);
                }
                CBT.AddToLogQueue($"Current gangway state: {CurrentGangwayState}", STULogType.INFO);
                if (CurrentGangwayState == GangwayStates.Retracted) CurrentGangwayState = GangwayStates.Extending;
                else if (CurrentGangwayState == GangwayStates.Extended) CurrentGangwayState = GangwayStates.Retracting;
            }
        }
    }
}
