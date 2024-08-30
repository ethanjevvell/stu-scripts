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
        bool RECALL;
        
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
            // I moved the logic that controlls the update frequency to the end Main() method so that when the ABORT keyword can be passed,
            // the program will do what it needs to do,
            // then it will set the update frequency to none.
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            RECALL = false;


        }

        public void Main(string argument, UpdateType updateSource)
        {
            maneuver = CBT.GenericManeuver;
            CBT.FlightController.UpdateState();
            
            argument = argument.Trim().ToUpper();
            
            // check whether the passed argument is a special command word, if it's not, default to parsing what the user passed
            switch (argument)
                {
                case "HALT":
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    CBT.AddToLogQueue("Halting ship locomotion...", STULogType.WARNING);
                    maneuver = CBT.Hover;
                    RECALL = false;
                    CBT.RemoteControl.DampenersOverride = true;
                    break;

                case "STOP":
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    CBT.AddToLogQueue("Slamming on the brakes...", STULogType.WARNING);
                    maneuver = CBT.FastStop;
                    RECALL = false;
                    CBT.RemoteControl.DampenersOverride = true;
                    break;

                case "CANCEL":
                    CBT.CurrentPhase = CBT.Phase.Idle;
                    CBT.AddToLogQueue("Cancelling last command recallability...", STULogType.WARNING);
                    RECALL = false;
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

                case "ABORT":
                    CBT.CurrentPhase = CBT.Phase.Idle;
                    RECALL = false;
                    CBT.RemoteControl.DampenersOverride = true;
                    CBT.AddToLogQueue("Attempting to relinquish control of the ship", STULogType.WARNING);
                    CBT.FlightController.RelinquishGyroControl();
                    CBT.FlightController.RelinquishThrusterControl();
                    CBT.AddToLogQueue($"Gyro control: {CBT.FlightController.HasGyroControl}", STULogType.OK);
                    CBT.AddToLogQueue($"Thruster control: {CBT.FlightController.HasThrusterControl}", STULogType.OK);
                    CBT.AddToLogQueue($"Dampeners: {CBT.RemoteControl.DampenersOverride}", STULogType.OK);
                    CBT.AddToLogQueue($"Recall: {RECALL}", STULogType.OK);

                    break;

                case "PARK":
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    RECALL = true;
                    CBT.RemoteControl.DampenersOverride = true;
                    CBT.FlightController.ReinstateGyroControl();
                    CBT.FlightController.ReinstateThrusterControl();

                    break;

                case "": // if the user passes nothing, do nothing
                    break;

                case "HELP":
                    CBT.AddToLogQueue("CBT Help Menu:", STULogType.OK);
                    CBT.AddToLogQueue("HALT - Executes a hover maneuver and then idles.", STULogType.OK);
                    CBT.AddToLogQueue("STOP - Same as HALT, but changes the ship's orientation to best counterract the current trajectory.", STULogType.OK);
                    CBT.AddToLogQueue("CANCEL - Cancels the last command recallability.", STULogType.OK);
                    CBT.AddToLogQueue("ABORT - Relinquishes control of the gyroscopes and thrusters, turns dampener override ON, and idles.", STULogType.OK);
                    CBT.AddToLogQueue("PARK - Orients the CBT to align with the Dock Ring and pulls forward to allow the pilot to manually dock.", STULogType.OK);
                    CBT.AddToLogQueue("AC130 - Not implemented yet.", STULogType.OK);
                    CBT.AddToLogQueue("TEST - Executes hard-coded maneuver parameters. FOR TESTING PURPOSES ONLY.", STULogType.OK);
                    CBT.AddToLogQueue("F5 - Move forward at 5m/s. \"B5 R4 Y0.5\" is the command to move backwards 5m/s, right 4m/s, and yaw 0.5 RPS", STULogType.OK);
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

            // check whether RECALL variable is set.
            // if true, then the last command will be executed again, essentially keeping the state machine in the same state.
            if (RECALL) { CBT.CurrentPhase = CBT.Phase.Executing; CBT.RemoteControl.DampenersOverride = false; }
            CBT.UpdateAutopilotScreens(RECALL);
            
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

            // bit of code to change the update frequency based on whether the autopilot is enabled.
            // BOTH gyros and thrusters have to be relinquised to the pilot to disable the autopilot
            if ((!CBT.FlightController.HasGyroControl) && (!CBT.FlightController.HasThrusterControl))
            {
                // stop updating the script automatically
                Runtime.UpdateFrequency = UpdateFrequency.None;

            }
            else
            {
                // if not both gyro and thruster control are enabled, update the script every 10 ticks
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            }

            // update the log screens
            CBT.UpdateLogScreens();
        }

        public bool ParseCommand(string arg)
        {
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

                        default:
                            CBT.AddToLogQueue($"Command {command} is not a valid direction qualifier. Skipping...", STULogType.WARNING);
                            break;
                    }
                } 
                RECALL = true;
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
