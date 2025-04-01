using Sandbox.Game.EntityComponents;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
// using static VRage.Game.VisualScripting.ScriptBuilder.MyVSAssemblyProvider;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        CBT CBTShip { get; set; }
        STUMasterLogBroadcaster Broadcaster { get; set; }
        IMyBroadcastListener Listener { get; set; }
        STUInventoryEnumerator InventoryEnumerator { get; set; }
        MyCommandLine CommandLineParser { get; set; } = new MyCommandLine();
        MyCommandLine WirelessMessageParser { get; set; } = new MyCommandLine();
        Queue<STUStateMachine> ManeuverQueue { get; set; } = new Queue<STUStateMachine>();
        STUStateMachine CurrentManeuver;
        public struct ManeuverQueueData
        {
            public string CurrentManeuverName;
            public bool CurrentManeuverInitStatus;
            public bool CurrentManeuverRunStatus;
            public bool CurrentManeuverCloseoutStatus;
            public string FirstManeuverName;
            public string SecondManeuverName;
            public string ThirdManeuverName;
            public string FourthManeuverName;
            public bool Continuation;
        }

        public Program()
        {
            // instantiate the actual CBT at the Program level so that all the methods in here will be directed towards a specific CBT object (the one that I fly around in game)
            Broadcaster = new STUMasterLogBroadcaster(CBT_VARIABLES.CBT_BROADCAST_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            Listener = IGC.RegisterBroadcastListener(CBT_VARIABLES.CBT_BROADCAST_CHANNEL);
            InventoryEnumerator = new STUInventoryEnumerator(GridTerminalSystem, Me);
            CBTShip = new CBT(Echo, Broadcaster, InventoryEnumerator, GridTerminalSystem, Me, Runtime);
            CBT.SetAutopilotControl(true, true, false);


            ResetAutopilot();

            // at compile time, Runtime.UpdateFrequency needs to be set to update every 10 ticks. 
            // I'm pretty sure the user input buffer is empty as far as the program is concerned whenever you hit recompile, even if there is text in the box.
            // i.e. it's only when you hit "run" does the program pull whatever is in the user input buffer and run it.
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                // fix and uncomment later
                // InventoryEnumerator.EnumerateInventories();

                HandleWirelessMessages();

                argument = argument.Trim().ToUpper();

                // parse the passed phrase as "[SUBJECT] [PREDICATE]"
                if (argument != "")
                {
                    if (!ParseCommand(argument))
                    {
                        CBT.AddToLogQueue($"cbt machine broke", STULogType.ERROR);
                    }
                }

                /// main state machine:
                /// Idle phase: look for work, so to speak. If there's something in the queue, pull it out and start executing it.
                /// Executing Phase: ultimately defers to the current maneuver's internal state machine.
                ///     if that internal state machine ever gets to the "Done" state, this "main" state machine will return to the Idle phase.
                switch (CBT.CurrentPhase)
                {
                    case CBT.Phase.Idle:
                        if (ManeuverQueue.Count > 0)
                        {
                            try
                            {
                                CurrentManeuver = ManeuverQueue.Dequeue();
                                CBT.AddToLogQueue($"Executing {CurrentManeuver.Name} maneuver...", STULogType.INFO);
                                CBT.CurrentPhase = CBT.Phase.Executing;
                            }
                            catch
                            {
                                CBT.AddToLogQueue("Could not pull maneuver from queue, despite the queue's count being greater than zero. Something is wrong, halting program...", STULogType.ERROR);
                                CBT.CreateBroadcast("MAYDAY MAYDAY MAYDAY", false, STULogType.ERROR);
                                Runtime.UpdateFrequency = UpdateFrequency.None;
                            }
                        }
                        break;

                    case CBT.Phase.Executing:
                        if (CurrentManeuver.ExecuteStateMachine())
                        {
                            CurrentManeuver = null;
                            CBT.CurrentPhase = CBT.Phase.Idle;
                        }
                        break;
                }

                // update various subsystems that are independent of the maneuver queue
                CBT.FlightController.UpdateState();
                CBT.Gangway.UpdateGangway(CBT.UserInputGangwayState);
                CBT.RearDock.UpdateRearDock();
                CBT.UpdateAutopilotScreens();
                CBT.UpdateLogScreens();
                CBT.UpdateManeuverQueueScreens(GatherManeuverQueueData());
                CBT.UpdateAmmoScreens();
                CBT.ACM.UpdateAirlocks();
                CBT.DockingModule.UpdateDockingModule();
                if (CBT.DockingModule.CurrentDockingModuleState == CBTDockingModule.DockingModuleStates.QueueManeuvers)
                {
                    try
                    {
                        // set up auxiliary hardware
                        CBT.Gangway.ToggleGangway(1);
                        CBT.UserInputRearDockPosition = 0;
                        CBT.MergeBlock.Enabled = true;
                        // go to point in space behind the CR
                        ManeuverQueue.Enqueue(new STUFlightController.GotoAndStop(CBT.FlightController, CBT.DockingModule.LineUpPosition, 10, CBT.MergeBlock));
                        // align ship to point at CR docking position
                        ManeuverQueue.Enqueue(new STUFlightController.PointAtTarget(CBT.FlightController, CBT.DockingModule.DockingPosition, CBT.MergeBlock));
                        // ManeuverQueue.Enqueue(new STUFlightController.PointAtTarget(CBT.FlightController, CBT.DockingModule.RollReference, CBT.MergeBlock, "down"));
                        // move again to line up with CR since the merge block is not at the center of mass
                        ManeuverQueue.Enqueue(new STUFlightController.GotoAndStop(CBT.FlightController, CBT.DockingModule.LineUpPosition, 2, CBT.MergeBlock));
                        // align again to point at CR docking position
                        ManeuverQueue.Enqueue(new STUFlightController.PointAtTarget(CBT.FlightController, CBT.DockingModule.DockingPosition, CBT.MergeBlock));
                        // ManeuverQueue.Enqueue(new STUFlightController.PointAtTarget(CBT.FlightController, CBT.DockingModule.RollReference, CBT.MergeBlock, "down"));
                        // move into docking position
                        ManeuverQueue.Enqueue(new STUFlightController.GotoAndStop(CBT.FlightController, CBT.DockingModule.DockingPosition, 7, CBT.MergeBlock));
                        // hover
                        ManeuverQueue.Enqueue(new CBT.HoverManeuver());
                        // ensure that this block is only hit once
                        CBT.DockingModule.CurrentDockingModuleState = CBTDockingModule.DockingModuleStates.Docking;
                    }
                    catch (Exception e)
                    {
                        CBT.AddToLogQueue($"Docking sequence failed: {e.Message}", STULogType.ERROR);
                    }
                }
            }

            catch (Exception e)
            {
                Echo($"Program.cs: Caught exception: {e} | source: {e.Source} | stacktrace: {e.StackTrace}");
                CBT.AddToLogQueue($"Program.cs: Caught exception: {e} | source: {e.Source} | stacktrace: {e.StackTrace}", STULogType.WARNING);
                CBT.AddToLogQueue("");
                CBT.AddToLogQueue("");
                CBT.AddToLogQueue("HALTING PROGRAM EXECUTION!", STULogType.ERROR);
                CBT.UpdateLogScreens();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        public ManeuverQueueData GatherManeuverQueueData()
        {
            ManeuverQueueData data = new ManeuverQueueData();
            data.CurrentManeuverName = CurrentManeuver?.Name;
            data.CurrentManeuverInitStatus = CurrentManeuver?.CurrentInternalState == STUStateMachine.InternalStates.Init;
            data.CurrentManeuverRunStatus = CurrentManeuver?.CurrentInternalState == STUStateMachine.InternalStates.Run;
            data.CurrentManeuverCloseoutStatus = CurrentManeuver?.CurrentInternalState == STUStateMachine.InternalStates.Closeout;
            data.FirstManeuverName = ManeuverQueue.Count > 0 ? ManeuverQueue.ElementAt(0).Name : null;
            data.SecondManeuverName = ManeuverQueue.Count > 1 ? ManeuverQueue.ElementAt(1).Name : null;
            data.ThirdManeuverName = ManeuverQueue.Count > 2 ? ManeuverQueue.ElementAt(2).Name : null;
            data.FourthManeuverName = ManeuverQueue.Count > 3 ? ManeuverQueue.ElementAt(3).Name : null;
            data.Continuation = ManeuverQueue.Count > 4;
            return data;
        }

        public void ResetAutopilot()
        {
            ManeuverQueue.Clear();
            CBT.ResetUserInputVelocities();
            foreach (var gyro in CBT.FlightController.AllGyroscopes)
            {
                gyro.Pitch = 0;
                gyro.Yaw = 0;
                gyro.Roll = 0;
            }
            CurrentManeuver = null;
            CBT.SetAutopilotControl(false, false, true);
            CBT.CurrentPhase = CBT.Phase.Idle;
        }

        public void HandleWirelessMessages()
        {
            if (Listener.HasPendingMessage)
            {
                var rawMessage = Listener.AcceptMessage();
                string message = rawMessage.Data.ToString();
                STULog incomingLog = STULog.Deserialize(message);
                // string decryptedMessage = Modem.Decrypt(incomingLog.Message, CBT_VARIABLES.TEA_KEY);

                if (WirelessMessageParser.TryParse(incomingLog.Message.ToUpper()))
                {
                    switch (WirelessMessageParser.Argument(0))
                    {
                        case "PING":
                            CBT.CreateBroadcast("PONG");
                            break;
                        case "POSITION":
                            CBT.AddToLogQueue($"Received position message: {incomingLog.Message}", STULogType.INFO);
                            try
                            {
                                double a = double.Parse(WirelessMessageParser.Argument(1).Trim());
                                double b = double.Parse(WirelessMessageParser.Argument(2).Trim());
                                double c = double.Parse(WirelessMessageParser.Argument(3).Trim());
                                double d = double.Parse(WirelessMessageParser.Argument(4).Trim());
                                double e = double.Parse(WirelessMessageParser.Argument(5).Trim());
                                double f = double.Parse(WirelessMessageParser.Argument(6).Trim());
                                double x = double.Parse(WirelessMessageParser.Argument(7).Trim());
                                double y = double.Parse(WirelessMessageParser.Argument(8).Trim());
                                double z = double.Parse(WirelessMessageParser.Argument(9).Trim());
                                CBT.DockingModule.LineUpPosition = new Vector3D(a, b, c);
                                CBT.DockingModule.RollReference = new Vector3D(d, e, f);
                                CBT.DockingModule.DockingPosition = new Vector3D(x, y, z);
                            }
                            catch (Exception e)
                            {
                                CBT.AddToLogQueue($"Failed to parse position message: {e.Message}", STULogType.ERROR);
                                Echo($"Failed to parse position message: {e.Message}");
                            }
                            break;
                        case "READY":
                            CBT.AddToLogQueue("Received READY message from Hyperdrive Ring.", STULogType.INFO);
                            CBT.DockingModule.CRReadyFlag = true;
                            break;
                        default:
                            CBT.AddToLogQueue($"Received message: {incomingLog.Message}", STULogType.INFO);
                            break;
                    }
                }
            }
        }


        public bool ParseCommand(string arg)
        {
            // commands MUST follow the structure "[SUBJECT] [PREDICATE]".
            if (CommandLineParser.TryParse(arg))
            {
                if (CommandLineParser.ArgumentCount % 2 != 0) // check parity of argument count.
                {
                    CBT.AddToLogQueue($"Command string '{arg}' does not have an even number of arguments! Refusing to parse.", STULogType.ERROR);
                    return false;
                }
                for (int i = 0; i < CommandLineParser.ArgumentCount; i = i + 2)
                {
                    string subject = CommandLineParser.Argument(i);
                    subject = subject.ToUpper();
                    if (subject.Length < 2)
                    {
                        CBT.AddToLogQueue($"Command '{subject}' is too short to be valid. Skipping...", STULogType.WARNING);
                        continue;
                    }

                    string predicate;
                    try
                    {
                        predicate = CommandLineParser.Argument(i + 1);
                        predicate = predicate.ToUpper();
                    }
                    catch
                    {
                        CBT.AddToLogQueue($"Could not parse the NOUN part of the command. Defaulting to a blank string.", STULogType.ERROR);
                        predicate = "";
                    }

                    float predicateAsFloat;
                    switch (subject)
                    {
                        case "FORWARD":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                CBT.UserInputForwardVelocity = predicateAsFloat;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "BACKWARD":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                CBT.UserInputForwardVelocity = Math.Abs(predicateAsFloat) * -1;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "UP":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                CBT.UserInputUpVelocity = predicateAsFloat;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "DOWN":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                CBT.UserInputUpVelocity = (predicateAsFloat) * -1;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "RIGHT":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                CBT.UserInputRightVelocity = predicateAsFloat;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "LEFT":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                CBT.UserInputRightVelocity = (predicateAsFloat) * -1;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "PITCH":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                if (-1 <= predicateAsFloat && predicateAsFloat <= 1)
                                {
                                    CBT.UserInputPitchVelocity = predicateAsFloat * 3.14f;
                                    ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                        CBT.UserInputForwardVelocity,
                                        CBT.UserInputRightVelocity,
                                        CBT.UserInputUpVelocity,
                                        CBT.UserInputRollVelocity,
                                        CBT.UserInputPitchVelocity,
                                        CBT.UserInputYawVelocity));
                                }
                                else { CBT.AddToLogQueue($"Pitch value '{predicateAsFloat}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "ROLL":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                if (-1 <= predicateAsFloat && predicateAsFloat <= 1)
                                {
                                    CBT.UserInputRollVelocity = predicateAsFloat * 3.14f;
                                    ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                        CBT.UserInputForwardVelocity,
                                        CBT.UserInputRightVelocity,
                                        CBT.UserInputUpVelocity,
                                        CBT.UserInputRollVelocity,
                                        CBT.UserInputPitchVelocity,
                                        CBT.UserInputYawVelocity));
                                }
                                else { CBT.AddToLogQueue($"Roll value '{predicateAsFloat}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "YAW":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                if (-1 <= predicateAsFloat && predicateAsFloat <= 1)
                                {
                                    CBT.UserInputYawVelocity = predicateAsFloat * 3.14f;
                                    ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                        CBT.UserInputForwardVelocity,
                                        CBT.UserInputRightVelocity,
                                        CBT.UserInputUpVelocity,
                                        CBT.UserInputRollVelocity,
                                        CBT.UserInputPitchVelocity,
                                        CBT.UserInputYawVelocity));
                                }
                                else { CBT.AddToLogQueue($"Yaw value '{predicateAsFloat}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "ALTITUDE":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                if (predicateAsFloat >= 25)
                                {
                                    CBT.AddToLogQueue($"Setting cruising altitude to {predicateAsFloat}m", STULogType.INFO);
                                    CBT.SetCruisingAltitude(predicateAsFloat);
                                }
                                else { CBT.AddToLogQueue($"Cruising altitude cannot be set lower than 25. Skipping...", STULogType.WARNING); }
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "SPEED":
                            if (float.TryParse(predicate, out predicateAsFloat))
                            {
                                if (predicateAsFloat > 0)
                                {
                                    CBT.AddToLogQueue($"Setting cruising speed to {predicateAsFloat}m/s", STULogType.INFO);
                                    ManeuverQueue.Enqueue(new CBT.CruisingSpeedManeuver(predicateAsFloat));
                                }
                                else { CBT.AddToLogQueue("Cruising speed must be a positive number. Skipping...", STULogType.WARNING); }
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "GANGWAY":
                            switch (predicate)
                            {
                                case "EXTEND":
                                    CBT.Gangway.ToggleGangway(1);
                                    break;
                                case "RETRACT":
                                    CBT.Gangway.ToggleGangway(0);
                                    break;
                                case "TOGGLE":
                                    CBT.Gangway.ToggleGangway();
                                    break;
                                case "RESET":
                                    CBT.UserInputGangwayState = CBTGangway.GangwayStates.Resetting;
                                    break;
                                default:
                                    CBT.AddToLogQueue($"Gangway command '{predicate}' is not valid. Try EXTEND, RETRACT, TOGGLE, or RESET. Skipping...", STULogType.WARNING);
                                    break;
                            }
                            break;

                        case "AIRLOCK":
                            switch (predicate)
                            {
                                case "ENABLE":
                                    CBT.ACM.EnableAutomaticControl();
                                    break;
                                case "DISABLE":
                                    CBT.ACM.DisableAuomaticControl();
                                    break;
                                default:
                                    CBT.AddToLogQueue($"Airlock command '{predicate}' is not valid. Try ENABLE or DISABLE. Skipping...", STULogType.WARNING);
                                    break;
                            }
                            break;

                        case "CLOSEOUT": // predicate is ignored, but generally must still have a value. Otherwise, it would be caught at the top of this method.
                            CBT.AddToLogQueue($"Cancelling maneuver '{CurrentManeuver.Name}'...", STULogType.INFO);
                            if (CurrentManeuver != null)
                            {
                                CurrentManeuver.CurrentInternalState = STUStateMachine.InternalStates.Closeout;
                            }
                            break;

                        case "HALT": // predicate is ignored, but generally must still have a value. Otherwise, it would be caught at the top of this method.
                            CBT.AddToLogQueue("Queueing a Hover maneuver", STULogType.INFO);
                            ManeuverQueue.Enqueue(new CBT.HoverManeuver());
                            break;

                        case "STOP": // predicate is ignored, but generally must still have a value. Otherwise, it would be caught at the top of this method.
                            CBT.AddToLogQueue("Queueing a fast stop maneuver", STULogType.INFO);
                            ManeuverQueue.Enqueue(new STUFlightController.HardStop(CBT.FlightController));
                            break;

                        case "AP":
                            switch (predicate)
                            {
                                case "RESET":
                                    CBT.AddToLogQueue("Resetting autopilot...", STULogType.INFO);
                                    ResetAutopilot();
                                    break;
                                default:
                                    CBT.AddToLogQueue($"Autopilot command '{predicate}' is not valid. Try RESET or DISABLE. Skipping...", STULogType.WARNING);
                                    break;
                            }
                            break;

                        case "PING": // predicate is ignored, but generally must still have a value. Otherwise, it would be caught at the top of this method.
                            CBT.AddToLogQueue("Broadcasting PING", STULogType.INFO);
                            CBT.CreateBroadcast("PING", false, STULogType.INFO);
                            break;

                        case "POSITION": // predicate is ignored, but generally must still have a value. Otherwise, it would be caught at the top of this method.
                            CBT.AddToLogQueue($"Current position:", STULogType.INFO);
                            CBT.AddToLogQueue($"X: {CBT.FlightController.CurrentPosition.X}", STULogType.INFO);
                            CBT.AddToLogQueue($"Y: {CBT.FlightController.CurrentPosition.Y}", STULogType.INFO);
                            CBT.AddToLogQueue($"Z: {CBT.FlightController.CurrentPosition.Z}", STULogType.INFO);
                            break;

                        case "DOCK":
                            switch (predicate)
                            {
                                case "REQUEST":
                                    CBT.AddToLogQueue("Sending dock request to Hyperdrive Ring...", STULogType.INFO);
                                    CBT.DockingModule.SendDockRequestFlag = true;
                                    break;
                                case "CONTINUE":
                                    CBT.DockingModule.PilotConfirmation = true;
                                    break;
                                case "CANCEL":
                                    if (CBT.DockingModule.CurrentDockingModuleState == CBTDockingModule.DockingModuleStates.ConfirmWithPilot)
                                    {
                                        CBT.AddToLogQueue("Docking sequence cancelled. Returning docking module state to idle...", STULogType.WARNING);
                                        CBT.CreateBroadcast("CANCEL");
                                        CBT.DockingModule.CurrentDockingModuleState = CBTDockingModule.DockingModuleStates.Idle;
                                    }
                                    else
                                    {
                                        CBT.AddToLogQueue("Didn't find anything to cancel. Skipping...", STULogType.WARNING);
                                    }
                                    break;
                                default:
                                    CBT.AddToLogQueue($"Docking command '{predicate}' is not valid. Try REQUEST, CONFIRM or CANCEL. Skipping...", STULogType.WARNING);
                                    break;
                            }
                            break;

                        case "STINGER":
                            switch (predicate)
                            {
                                case "RESET":
                                    CBT.UserInputRearDockPosition = 1;
                                    break;
                                case "STOW":
                                    CBT.UserInputRearDockPosition = 0;
                                    break;
                                case "LHQ":
                                    CBT.UserInputRearDockPosition = 2;
                                    break;
                                case "HEROBRINE":
                                    CBT.UserInputRearDockPosition = 3;
                                    break;
                                case "CLAM":
                                    CBT.UserInputRearDockPosition = 4;
                                    break;
                                default:
                                    CBT.AddToLogQueue($"Stinger command '{predicate}' is not valid. Try RESET, STOW, LHQ, HEROBRINE, or CLAM. Skipping...", STULogType.WARNING);
                                    break;
                            }
                            break;

                        case "POWER":
                            int powerLevel;
                            if (int.TryParse(predicate, out powerLevel))
                            {
                                if (0 <= powerLevel && powerLevel <= 7)
                                {
                                    CBT.SetPowerLevel(powerLevel);
                                    break;
                                }
                                else
                                {
                                    CBT.AddToLogQueue($"Power level {powerLevel} is out of range. Must be between 0 and 7 (inclusive).");
                                    break;
                                }
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Failed to parse noun {predicate} on verb {subject}. Skipping...");
                            }
                            break;

                        case "HELP":
                            switch (predicate)
                            {
                                case "":
                                    CBT.AddToLogQueue("Enter a page number [1 - 3] to view detailed help. Commands are organized alphabetically.", STULogType.OK);
                                    CBT.AddToLogQueue("CLOSEOUT {NOW} - Immediately goes to the 'closeout' state of the current maneuver.", STULogType.OK);
                                    CBT.AddToLogQueue("HALT {NOW} - Executes a hover maneuver.", STULogType.OK);
                                    CBT.AddToLogQueue("STOP {NOW} - Same as HALT, but changes the ship's orientation before firing thrusters to best counterract the current trajectory.", STULogType.OK);
                                    CBT.AddToLogQueue("AP RESET - Resets the autopilot 'manually' (outisde of the maneuver queue) and clears the maneuver queue.", STULogType.OK);
                                    CBT.AddToLogQueue("TEST - Executes hard-coded maneuver parameters. FOR TESTING PURPOSES ONLY.", STULogType.OK);
                                    break;
                                default:
                                    CBT.AddToLogQueue("Page number out of range (probably not implemented yet).");
                                    break;
                            }
                            break;


                        default:
                            CBT.AddToLogQueue($"Unrecognized subject '{subject}'. Skipping...", STULogType.WARNING);
                            break;
                    }
                }
                return true;
            }
            else
            {
                CBT.AddToLogQueue($"damn this shit broken fr fr", STULogType.ERROR); return false;
            }
        }
    }
}
