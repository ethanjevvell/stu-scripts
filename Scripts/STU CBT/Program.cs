using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        MyCommandLine CommandLineParser = new MyCommandLine();
        Func<bool> maneuver;
        
        public Program()
        {
            // instantiate the actual CBT at the Program level so that all the methods in here will be directed towards a specific CBT object (the one that I fly around in game)
            Broadcaster = new STUMasterLogBroadcaster(CBT_VARIABLES.CBT_BROADCAST_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            CBTShip = new CBT(Echo, Broadcaster, GridTerminalSystem, Me, Runtime);
            CBT.FlightController.ReinstateGyroControl();
            CBT.FlightController.ReinstateThrusterControl();

            // at compile time, Runtime.UpdateFrequency needs to be set to update every 10 ticks. 
            // I'm pretty sure the user input buffer is empty as far as the program is concerned whenever you hit recompile, even if there is text in the box.
            // i.e. it's only when you hit "run" does the program pull whatever is in the user input buffer and run it.
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                // maneuver = CBT.GenericManeuver;

                

                argument = argument.Trim().ToUpper();

                // check whether the passed argument is a special command word, if it's not, hand it off to the command parser
                switch (argument)
                {
                    case "HALT":
                        CBT.CurrentPhase = CBT.Phase.Executing;
                        CBT.AddToLogQueue("Halting ship locomotion...", STULogType.WARNING);
                        maneuver = CBT.Hover;
                        break;

                    case "STOP":
                        // CBT.CurrentPhase = CBT.Phase.Executing;
                        CBT.AddToLogQueue("Stop command not implemented yet.", STULogType.WARNING);
                        // maneuver = CBT.FastStop;
                        break;

                    case "CANCEL":
                        CBT.CurrentPhase = CBT.Phase.Idle;
                        CBT.AddToLogQueue("Breaking out of main state machine...", STULogType.WARNING);
                        break;

                    case "AC130":
                        // CBT.CurrentPhase = CBT.Phase.Executing;

                        CBT.AddToLogQueue("AC130 command not implemented yet.", STULogType.ERROR);
                        break;

                    case "TEST": // should only be used for testing purposes. hard-code stuff here
                        CBT.CurrentPhase = CBT.Phase.Executing;
                        CBT.FlightController.ReinstateGyroControl();
                        CBT.FlightController.ReinstateThrusterControl();
                        CBT.NextWaypoint = new Vector3D(0, 0, 0);
                        maneuver = CBT.PointAtTarget;
                        break;

                    case "ABORT": // keeping this stuff top-level for readability (rather than buried in the CBT class)
                        CBT.CurrentPhase = CBT.Phase.Idle;
                        CBT.RemoteControl.DampenersOverride = true;
                        CBT.AddToLogQueue("Attempting to relinquish control of the ship...", STULogType.WARNING);
                        CBT.FlightController.RelinquishGyroControl();
                        CBT.FlightController.RelinquishThrusterControl();
                        CBT.AddToLogQueue($"Gyro control: {CBT.FlightController.HasGyroControl}", STULogType.INFO);
                        CBT.AddToLogQueue($"Thruster control: {CBT.FlightController.HasThrusterControl}", STULogType.INFO);
                        CBT.AddToLogQueue($"Dampeners: {CBT.RemoteControl.DampenersOverride}", STULogType.INFO);

                        break;

                    case "GANGWAY":
                        CBT.Gangway.ToggleGangway();
                        break;

                    case "GE":
                        CBT.Gangway.ToggleGangway(1);
                        break;

                    case "GR":
                        CBT.Gangway.ToggleGangway(0);
                        break;

                    case "GANGWAYRESET":
                        CBT.UserInputGangwayState = CBTGangway.GangwayStates.Resetting;
                        break;

                    case "STINGER":
                        CBT.RearDock.DestinationAfterNeutral = CBTRearDock.RearDockStates.Retracted;
                        CBT.UserInputRearDockState = CBTRearDock.RearDockStates.Resetting;
                        break;

                    case "PARK":
                        CBT.CurrentPhase = CBT.Phase.Executing;
                        CBT.SetAutopilotControl(true, true, true);
                        break;

                    case "": // if the user passes nothing, do nothing
                        break;

                    case "HELP":
                        CBT.AddToLogQueue("CBT Help Menu:", STULogType.OK);
                        CBT.AddToLogQueue("HALT - Executes a hover maneuver.", STULogType.OK);
                        CBT.AddToLogQueue("STOP - Same as HALT, but changes the ship's orientation before firing thrusters to best counterract the current trajectory.", STULogType.OK);
                        CBT.AddToLogQueue("CANCEL - Freezes all actuators and changes the state of the Flight Controller to IDLE, UNgracefully.", STULogType.OK);
                        CBT.AddToLogQueue("ABORT - Freezes all actuators, relinquishes control of the gyroscopes and thrusters, turns dampener override ON, and idles the Flight Controller.", STULogType.OK);
                        CBT.AddToLogQueue("PARK - Orients the CBT to align with the Dock Ring and pulls forward to allow the pilot to manually dock.", STULogType.OK);
                        CBT.AddToLogQueue("AC130 - Not implemented yet.", STULogType.OK);
                        CBT.AddToLogQueue("TEST - Executes hard-coded maneuver parameters. FOR TESTING PURPOSES ONLY.", STULogType.OK);
                        CBT.AddToLogQueue("(enter 'helpp' to see an explanation of Generic Maneuvers)", STULogType.OK);
                        break;

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
                        break;

                    default:
                        CBT.AddToLogQueue($"User input: {argument}");
                        if (ParseCommand(argument))
                        {
                            CBT.CurrentPhase = CBT.Phase.Executing;
                            CBT.AddToLogQueue($"Executing {maneuver}");
                        }
                        break;
                }


                /// main state machine
                switch (CBT.CurrentPhase)
                {
                    case CBT.Phase.Idle:
                        break;
                    case CBT.Phase.Executing:
                        var finishedExecuting = maneuver();
                        if (finishedExecuting)
                        {
                            CBT.AddToLogQueue($"hit idle stage", STULogType.OK);
                            CBT.CurrentPhase = CBT.Phase.Idle;
                            // set all thruster outputs to zero (in certain cases, the thing we talked about with Ethan about residual velocity on a previous tick)
                        }
                        break;
                }

                // update various subsystems
                CBT.FlightController.UpdateState();
                CBT.Gangway.UpdateGangway(CBT.UserInputGangwayState);
                CBT.RearDock.UpdateRearDock(CBT.UserInputRearDockState);
                CBT.UpdateAutopilotScreens();
                CBT.UpdateLogScreens();
            }
            catch (Exception e)
            {
                Echo($"Program.cs: Caught exception: {e}");
                CBT.AddToLogQueue($"Caught exception: {e}", STULogType.ERROR);
                CBT.UpdateLogScreens();
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
                        CBT.AddToLogQueue($"Command: {command} is too short to be valid. Skipping...", STULogType.WARNING);
                        continue;
                    }

                    char direction = command[0];
                    float result;
                    float value = float.TryParse(command.Substring(1), out result) ? result : 0;
                    
                    switch (direction)
                    {
                        case 'F':
                            CBT.UserInputForwardVelocity = value;
                            maneuver = CBT.GenericManeuver;
                            break;

                        case 'B':
                            CBT.UserInputForwardVelocity = (value) * -1;
                            maneuver = CBT.GenericManeuver;
                            break;

                        case 'U':
                            CBT.UserInputUpVelocity = value;
                            maneuver = CBT.GenericManeuver;
                            break;

                        case 'D':
                            CBT.UserInputUpVelocity = (value) * -1;
                            maneuver = CBT.GenericManeuver;
                            break;

                        case 'R':
                            CBT.UserInputRightVelocity = value;
                            maneuver = CBT.GenericManeuver;
                            break;

                        case 'L':
                            CBT.UserInputRightVelocity = (value) * -1;
                            maneuver = CBT.GenericManeuver;
                            break;

                        case 'P':
                            if (-1 <= value && value <= 1) { CBT.UserInputPitchVelocity = value * 3.14f * 0.5f; maneuver = CBT.GenericManeuver; }
                            else {
                                CBT.AddToLogQueue($"Pitch value {value} is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING);
                            }
                            break;

                        case 'H':
                            if (-1 <= value && value <= 1) { CBT.UserInputPitchVelocity = value * 3.14f * 0.5f * -1; maneuver = CBT.GenericManeuver; }
                            else
                            {
                                CBT.AddToLogQueue($"Pitch value {value} is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING);
                            }
                            break;

                        case 'O':
                            if (-1 <= value && value <= Math.PI) { CBT.UserInputRollVelocity = value * 3.14f; maneuver = CBT.GenericManeuver; }
                            else
                            {
                                CBT.AddToLogQueue($"Roll value {value} is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING);
                            }
                            break;

                        case 'Q':
                            if (-1 <= value && value <= 1) { CBT.UserInputRollVelocity = value * 3.14f * -1; maneuver = CBT.GenericManeuver; }
                            else
                            {
                                CBT.AddToLogQueue($"Roll value {value} is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING);
                            }
                            break;

                        case 'Y':
                            if (-1 <= value && value <= 1) { CBT.UserInputYawVelocity = value * 3.14f; maneuver = CBT.GenericManeuver; }
                            else
                            {
                                CBT.AddToLogQueue($"Yaw value {value} is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING);
                            }
                            break;

                        case 'W':
                            if (-1 <= value && value <= 1) { CBT.UserInputYawVelocity = value * 3.14f * -1; maneuver = CBT.GenericManeuver; }
                            else
                            {
                                CBT.AddToLogQueue($"Yaw value {value} is out of range. Must be between -1 and +1. Skipping...", STULogType.WARNING);
                            }
                            break;

                        case 'A':
                            if (0 <= value) { 
                                CBT.AddToLogQueue($"Setting cruising altitude to {value}m", STULogType.INFO);
                                CBT.CruisingAltitude(value); }
                            else { CBT.AddToLogQueue($"Altitude value {value} is out of range. Must be greater than 0. Skipping...", STULogType.WARNING);                    }
                            break;

                        case 'C':
                            CBT.AddToLogQueue($"Setting cruising speed to {value} m/s", STULogType.INFO);
                            CBT.UserInputForwardVelocity = value;
                            CBT.FlightController.RelinquishGyroControl();
                            maneuver = CBT.CruisingSpeed;
                            break;

                        case 'G':
                            CBT.AddToLogQueue($"Gangway command value {value}", STULogType.INFO);
                            if (value == 0 || value == 1)
                            {
                                CBT.Gangway.ToggleGangway(value);
                            }
                            else
                            {
                                CBT.AddToLogQueue($"Gangway command value {value} is not valid. Must be 0 to retract or 1 to extend, or use full name 'gangway' to toggle. Skipping...", STULogType.WARNING);
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
                CBT.AddToLogQueue($"Could not parse command string \"{arg}\" which was passed to ParseCommand()", STULogType.WARNING);
                return false;
            }
        }
    }
}
