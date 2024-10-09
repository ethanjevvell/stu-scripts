using Sandbox.Game.EntityComponents;
using Sandbox.Game.Screens.Helpers;
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

        CBT CBTShip;
        STUMasterLogBroadcaster Broadcaster;
        IMyBroadcastListener Listener;
        MyCommandLine CommandLineParser = new MyCommandLine();
        Queue<STUStateMachine> ManeuverQueue = new Queue<STUStateMachine>();
        STUStateMachine CurrentManeuver;
        TEA Modem = new TEA();
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
            CBTShip = new CBT(Echo, Broadcaster, GridTerminalSystem, Me, Runtime);
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
                HandleWirelessMessages();
                
                argument = argument.Trim().ToUpper();

                // check whether the passed argument is a special command word, if it's not, hand it off to the command parser
                if (argument != "")
                {
                    if (!CheckSpecialCommandWord(argument))
                    {
                        if (!ParseCommand(argument))
                        {
                            CBT.AddToLogQueue($"cbt machine broke", STULogType.ERROR);
                        }
                    }
                }
                
                /// main state machine:
                /// Idle phase: look for work, so to speak. If there's something in the queue, pull it out and start executing it.
                /// Executing Phase: ultimately defers to the current maneuver's internal state machine.
                ///     if it ever gets to the "Done" state, it will return to the Idle phase.
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
                CBT.RearDock.UpdateRearDock(CBT.UserInputRearDockState);
                CBT.UpdateAutopilotScreens();
                CBT.UpdateLogScreens();
                CBT.UpdateManeuverQueueScreens(GatherManeuverQueueData());
                CBT.UpdateAmmoScreens();
            }

            catch (Exception e)
            {
                Echo($"Program.cs: Caught exception: {e}\n");
                CBT.AddToLogQueue($"Caught exception: {e}\n", STULogType.ERROR);
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
            CBT.SetAutopilotControl(false, false, true);
            ManeuverQueue.Clear();
            CBT.ResetUserInputVelocities();
            CurrentManeuver = null;
            CBT.CurrentPhase = CBT.Phase.Idle;
        }

        public void HandleWirelessMessages()
        {
            if (Listener.HasPendingMessage)
            {
                CBT.AddToLogQueue("Received something lol", STULogType.INFO);
                var rawMessage = Listener.AcceptMessage();
                string message = rawMessage.Data.ToString();
                STULog incomingLog = STULog.Deserialize(message);
                string decryptedMessage = Modem.Decrypt(incomingLog.Message, CBT_VARIABLES.TEA_KEY);
                
                CBT.AddToLogQueue($"Received message: {decryptedMessage}", STULogType.INFO);

                if (decryptedMessage == "PING")
                {
                    CBT.AddToLogQueue("PONG", STULogType.OK);
                    CBT.CreateBroadcast("PONG", true, STULogType.OK);
                }
            }
        }

        public bool CheckSpecialCommandWord(string arg)
        {
            switch (arg)
            {
                case "CLOSEOUT":
                    CBT.AddToLogQueue($"Cancelling maneuver '{CurrentManeuver.Name}'...", STULogType.INFO);
                    if (CurrentManeuver != null)
                    {
                        CurrentManeuver.CurrentInternalState = STUStateMachine.InternalStates.Closeout;
                    }
                    return true;
                case "HALT":
                    CBT.AddToLogQueue("Queueing a Hover maneuver", STULogType.INFO);
                    ManeuverQueue.Enqueue(new CBT.HoverManeuver());
                    return true;
                case "STOP":
                    CBT.AddToLogQueue("Queueing a fast stop maneuver", STULogType.INFO);
                    ManeuverQueue.Enqueue(new STUFlightController.HardStop(CBT.FlightController));
                    return true;
                case "RESETAP":
                    CBT.AddToLogQueue("Resetting autopilot...", STULogType.INFO);
                    ResetAutopilot();
                    return true;
                case "AC130":
                    CBT.AddToLogQueue("AC130 command not implemented yet.", STULogType.ERROR);
                    return true;

                case "TEST": // should only be used for testing purposes. hard-code stuff in the test maneuver.
                    CBT.AddToLogQueue("Performing test", STULogType.INFO);
                    CBT.CreateBroadcast("PING", true, STULogType.OK);
                    //ManeuverQueue.Enqueue(new STUFlightController.GotoAndStop(CBT.FlightController, STUGalacticMap.Waypoints.GetValueOrDefault("CBT"), 10));
                    //ManeuverQueue.Enqueue(new STUFlightController.GotoAndStop(CBT.FlightController, STUGalacticMap.Waypoints.GetValueOrDefault("CBT2"), 20));
                    return true;

                case "GANGWAY":
                    CBT.Gangway.ToggleGangway();
                    return true;
                case "GE":
                    CBT.Gangway.ToggleGangway(1);
                    return true;
                case "GR":
                    CBT.Gangway.ToggleGangway(0);
                    return true;
                case "GANGWAYRESET":
                    CBT.UserInputGangwayState = CBTGangway.GangwayStates.Resetting;
                    return true;
                case "STINGER":
                    return true;
                case "PARK":
                    CBT.AddToLogQueue("Park maneuver not implemented yet.", STULogType.ERROR);
                    return true;
                case "HELP":
                    CBT.AddToLogQueue("CBT Help Menu:", STULogType.OK);
                    CBT.AddToLogQueue("CLOSEOUT - Immediately goes to the 'closeout' state of the current maneuver.", STULogType.OK);
                    CBT.AddToLogQueue("HALT - Executes a hover maneuver.", STULogType.OK);
                    CBT.AddToLogQueue("STOP - Same as HALT, but changes the ship's orientation before firing thrusters to best counterract the current trajectory.", STULogType.OK);
                    CBT.AddToLogQueue("RESETAP - Resets the autopilot 'manually' (outisde of the maneuver queue) and clears the maneuver queue.", STULogType.OK);
                    CBT.AddToLogQueue("PARK - Orients the CBT to align with the Hyperdrive Ring and pulls forward to allow the pilot to manually dock.", STULogType.OK);
                    CBT.AddToLogQueue("AC130 - Not implemented yet.", STULogType.OK);
                    CBT.AddToLogQueue("TEST - Executes hard-coded maneuver parameters. FOR TESTING PURPOSES ONLY.", STULogType.OK);
                    CBT.AddToLogQueue("(enter 'helpp' to see an explanation of Generic Maneuvers)", STULogType.OK);
                    return true;
                case "HELPP":
                    CBT.AddToLogQueue("Generic Maneuvers:", STULogType.OK);
                    CBT.AddToLogQueue("F5 - Move forward at 5m/s", STULogType.OK);
                    CBT.AddToLogQueue("B5 - Move backward at 5m/s", STULogType.OK);
                    CBT.AddToLogQueue("U5 - Move up at 5m/s", STULogType.OK);
                    CBT.AddToLogQueue("D5 - Move down at 5m/s", STULogType.OK);
                    CBT.AddToLogQueue("R5 - Move right at 5m/s", STULogType.OK);
                    CBT.AddToLogQueue("L5 - Move left at 5m/s", STULogType.OK);
                    CBT.AddToLogQueue("P1 - Pitch up at 1 rad/s", STULogType.OK);
                    CBT.AddToLogQueue("H1 - Pitch down at 1 rad/s", STULogType.OK);
                    CBT.AddToLogQueue("O1 - Roll clockwise at 1 rad/s", STULogType.OK);
                    CBT.AddToLogQueue("Q1 - Roll counter-clockwise at 1 rad/s", STULogType.OK);
                    CBT.AddToLogQueue("Y1 - Yaw right at 1 rad/s", STULogType.OK);
                    CBT.AddToLogQueue("W1 - Yaw left at 1 rad/s", STULogType.OK);
                    CBT.AddToLogQueue("A100 - Set cruising altitude to 100m", STULogType.OK);
                    CBT.AddToLogQueue("C10 - Set cruising speed to 10m/s", STULogType.OK);
                    CBT.AddToLogQueue("G0 - Retract gangway", STULogType.OK);
                    CBT.AddToLogQueue("G1 - Extend gangway", STULogType.OK);
                    return true;
                default:
                    CBT.AddToLogQueue($"String \"{arg}\" was not a special command word. Handing off to ParseCommand()...", STULogType.WARNING);
                    return false;
            }
        }

        public bool ParseCommand(string arg)
        {
            CBT.AddToLogQueue($"Parsing command string \"{arg}\"", STULogType.INFO);
            /// code to break up space-separated commands that might be entered into the terminal.
            /// e.g. "F5" should be interpreted as "move forward at 5m/s"
            /// "F5 D1" should be interpreted as "move forward 5m/s AND down 1m/s"
            /// "R5 Y-1" should be interpreted as "move right at 5m/s and yaw left at 1 degree per second"
            /// F = move forward
            /// B = move backwards
            /// U = move up
            /// D = move down
            /// R = move right
            /// L = move left
            /// P = pitch, positive number = pitch up, negative number = pitch down
            /// H = pitch down (?)
            /// O = roll, positive number = roll clockwise wrt forward axis, negative number = roll counter-clockwise wrt forward axis
            /// Q = roll counter-clockwise (?)
            /// Y = yaw, positive number = yaw right, negative number = yaw left
            /// W = yaw left

            // loop through the passed string and act on valid direction qualifiers (listed above)
            if (CommandLineParser.TryParse(arg))
            {
                for (int i = 0; i < CommandLineParser.ArgumentCount; i++)
                {
                    string command = CommandLineParser.Argument(i);
                    if (command.Length < 2)
                    {
                        CBT.AddToLogQueue($"Command '{command}' is too short to be valid. Skipping...", STULogType.WARNING);
                        continue;
                    }

                    char direction = command[0];
                    string secondCharacter = command.Substring(1);
                    try
                    {
                        float.Parse(secondCharacter);
                    }
                    catch (Exception e)
                    {
                        Echo($"EXCEPTION: {e.Message}");
                        CBT.AddToLogQueue($"EXCEPTION: {e.Message}");
                        break;
                    }
                    float result;
                    float value = float.TryParse(command.Substring(1), out result) ? result : 0;
                    
                    switch (direction)
                    {
                        case 'F':
                            CBT.UserInputForwardVelocity = value;
                            ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                CBT.UserInputForwardVelocity,
                                CBT.UserInputRightVelocity,
                                CBT.UserInputUpVelocity,
                                CBT.UserInputRollVelocity,
                                CBT.UserInputPitchVelocity,
                                CBT.UserInputYawVelocity));
                            break;

                        case 'B':
                            CBT.UserInputForwardVelocity = (value) * -1;
                            ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                CBT.UserInputForwardVelocity, 
                                CBT.UserInputRightVelocity, 
                                CBT.UserInputUpVelocity, 
                                CBT.UserInputRollVelocity, 
                                CBT.UserInputPitchVelocity, 
                                CBT.UserInputYawVelocity));
                            break;

                        case 'U':
                            CBT.UserInputUpVelocity = value;
                            ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                CBT.UserInputForwardVelocity,
                                CBT.UserInputRightVelocity,
                                CBT.UserInputUpVelocity,
                                CBT.UserInputRollVelocity,
                                CBT.UserInputPitchVelocity,
                                CBT.UserInputYawVelocity));
                            break;

                        case 'D':
                            CBT.UserInputUpVelocity = (value) * -1;
                            ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                CBT.UserInputForwardVelocity,
                                CBT.UserInputRightVelocity,
                                CBT.UserInputUpVelocity,
                                CBT.UserInputRollVelocity,
                                CBT.UserInputPitchVelocity,
                                CBT.UserInputYawVelocity));
                            break;

                        case 'R':
                            CBT.UserInputRightVelocity = value;
                            ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                CBT.UserInputForwardVelocity,
                                CBT.UserInputRightVelocity,
                                CBT.UserInputUpVelocity,
                                CBT.UserInputRollVelocity,
                                CBT.UserInputPitchVelocity,
                                CBT.UserInputYawVelocity));
                            break;

                        case 'L':
                            CBT.UserInputRightVelocity = (value) * -1;
                            ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                CBT.UserInputForwardVelocity,
                                CBT.UserInputRightVelocity,
                                CBT.UserInputUpVelocity,
                                CBT.UserInputRollVelocity,
                                CBT.UserInputPitchVelocity,
                                CBT.UserInputYawVelocity));
                            break;

                        case 'P':
                            if (-1 <= value && value <= 1) 
                            { 
                                CBT.UserInputPitchVelocity = value * 3.14f * 0.5f;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else { CBT.AddToLogQueue($"Pitch value '{value}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            break;

                        case 'H':
                            if (-1 <= value && value <= 1) 
                            { 
                                CBT.UserInputPitchVelocity = value * 3.14f * 0.5f * -1;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else { CBT.AddToLogQueue($"Pitch value '{value}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            break;

                        case 'O':
                            if (-1 <= value && value <= Math.PI) 
                            { 
                                CBT.UserInputRollVelocity = value * 3.14f;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else { CBT.AddToLogQueue($"Roll value '{value}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            break;

                        case 'Q':
                            if (-1 <= value && value <= 1) 
                            { 
                                CBT.UserInputRollVelocity = value * 3.14f * -1;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else { CBT.AddToLogQueue($"Roll value '{value}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            break;

                        case 'Y':
                            if (-1 <= value && value <= 1) 
                            { 
                                CBT.UserInputYawVelocity = value * 3.14f;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else { CBT.AddToLogQueue($"Yaw value '{value}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            break;

                        case 'W':
                            if (-1 <= value && value <= 1) 
                            { 
                                CBT.UserInputYawVelocity = value * 3.14f * -1;
                                ManeuverQueue.Enqueue(new CBT.GenericManeuver(
                                    CBT.UserInputForwardVelocity,
                                    CBT.UserInputRightVelocity,
                                    CBT.UserInputUpVelocity,
                                    CBT.UserInputRollVelocity,
                                    CBT.UserInputPitchVelocity,
                                    CBT.UserInputYawVelocity));
                            }
                            else { CBT.AddToLogQueue($"Yaw value '{value}' is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING); }
                            break;

                        case 'A':
                            if (0 <= value) { 
                                CBT.AddToLogQueue($"Setting cruising altitude to {value}m", STULogType.INFO);
                                CBT.SetCruisingAltitude(value); }
                            else { CBT.AddToLogQueue($"Altitude value '{value}' is out of range. Must be greater than 0. Skipping...", STULogType.WARNING);                    }
                            break;

                        case 'C':
                            CBT.AddToLogQueue($"Setting cruising speed to {value}m/s", STULogType.INFO);
                            ManeuverQueue.Enqueue(new CBT.CruisingSpeedManeuver(value));
                            break;

                        case 'G':
                            CBT.AddToLogQueue($"Gangway command value {value}", STULogType.INFO);
                            if (value == 0 || value == 1)
                            {
                                CBT.Gangway.ToggleGangway(value);
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Gangway command value '{value}' is not valid. Must be 0 to retract or 1 to extend, or use full name 'gangway' to toggle. Skipping...", STULogType.WARNING);
                            }
                            break;

                        default:
                            CBT.AddToLogQueue($"Command letter {command} is not a valid operator. Skipping...", STULogType.WARNING);
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
