﻿using Sandbox.Game.EntityComponents;
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
        
        public Program()
        {
            // instantiate the actual CBT at the Program level so that all the methods in here will be directed towards a specific CBT object (the one that I fly around in game)
            Broadcaster = new STUMasterLogBroadcaster(CBT_VARIABLES.CBT_BROADCAST_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            CBTShip = new CBT(Broadcaster, GridTerminalSystem, Me, Runtime);

            // at compile time, Runtime.UpdateFrequency needs to be set to update every 10 ticks. 
            // I'm pretty sure the user input buffer is empty as far as the program is concerned whenever you hit recompile, even if there is text in the box.
            // i.e. it's only when you hit "run" does the program pull whatever is in the user input buffer and run it.
            // I moved the logic that controlls the update frequency to the end Main() method so that the ABORT keyword can be passed, the program will do what it needs to do,
            // then it will set the update frequency to none.
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

        }

        public void Main(string argument, UpdateType updateSource)
        {
            CBT.FlightController.GiveControl();
            CBT.FlightController.UpdateState();
            Func<bool> maneuver = CBT.GenericManeuver;

            argument = argument.Trim().ToUpper();
            CBT.CreateBroadcast($"attempting to parse command: {argument}", STULogType.OK);
            
            // check whether the passed argument is a special command word, if it's not, default to parsing what the user passed
            switch (argument)
                {
                case "STOP":
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    maneuver = CBT.Hover;
                    break;

                case "AC130":
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    // CBT.AC130(100, CBT.GetCurrentXCoord, CBT.GetCurrentYCoord, CBT.GetCurrentZCoord, 60);
                    break;

                case "TEST": // should only be used for testing purposes. hard-code stuff here
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    CBT.UserInputForwardVelocity = 0;
                    CBT.UserInputRightVelocity = 0;
                    CBT.UserInputUpVelocity = 0;
                    CBT.UserInputPitchVelocity = 0;
                    CBT.UserInputRollVelocity = 0;
                    CBT.UserInputYawVelocity = 1;
                    maneuver = CBT.GenericManeuver;
                    break;

                case "ABORT":
                    CBT.CurrentPhase = CBT.Phase.Idle;
                    Echo("attempting to relinquish control of the ship");
                    CBT.FlightController.RelinquishControl();

                    break;

                default:
                    CBT.CreateBroadcast($"command {argument} passed to the main switch block could not be found in the list of special commands.", STULogType.WARNING);
                    if (ParseCommand(argument))
                    {
                        CBT.CurrentPhase = CBT.Phase.Executing;
                        maneuver = CBT.GenericManeuver;
                    }
                    break;
                }

            /// main state machine
            switch (CBT.CurrentPhase)
            {
                case CBT.Phase.Idle:
                    break;
                case CBT.Phase.Executing:
                    CBT.CreateBroadcast("CBT is executing a command", STULogType.OK);
                    var finishedExecuting = maneuver();
                    if (finishedExecuting)
                    {
                        CBT.CreateBroadcast("CBT has finished executing the command", STULogType.OK);
                        CBT.CurrentPhase = CBT.Phase.Idle;
                    }
                    break;
            }

            // bit of code to change the update frequency based on whether the autopilot is enabled (FlightController.HasControl is set to true)
            // also update the autopilot status screens
            if (CBT.FlightController.HasControl)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;

            }
            else
            {
                Echo("setting update frequency to none");
                // stop updating the script automatically
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }

            // update the log screens
            CBT.UpdateLogScreens();

            // hacky checks below
            Echo($"{CBT.LogChannel.Count}");
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
            Echo($"Attempting to parse command: {arg}");
            if (CommandLineParser.TryParse(arg))
            {
                for (int i = 0; i < CommandLineParser.ArgumentCount; i++)
                {
                    string command = CommandLineParser.Argument(i);
                    if (command.Length < 2)
                    {
                        CBT.CreateBroadcast($"Command: {command} is too short to be valid. Skipping...", STULogType.WARNING);
                        continue;
                    }
                    else
                    {
                        CBT.CreateBroadcast($"Command: {command} is fucked up. Aborting...", STULogType.ERROR);
                    }

                    char direction = command[0];
                    float result;
                    float value = float.TryParse(command.Substring(1), out result) ? result : 0;
                    
                    switch (direction)
                    {
                        case 'F':
                            CBT.UserInputForwardVelocity = value;
                            break;

                        case 'B':
                            CBT.UserInputForwardVelocity = (value) * -1;
                            break;

                        case 'U':
                            CBT.UserInputUpVelocity = value;
                            break;

                        case 'D':
                            CBT.UserInputUpVelocity = (value) * -1;
                            break;

                        case 'R':
                            CBT.UserInputRightVelocity = value;
                            break;

                        case 'L':
                            CBT.UserInputRightVelocity = (value) * -1;
                            break;

                        case 'P':
                            if (-1 <= value && value <= 1) { CBT.UserInputPitchVelocity = value; }
                            else {
                                CBT.CreateBroadcast($"Pitch value {value} is out of range. Must be between -1 and 1. Setting to 0...", STULogType.WARNING);
                                CBT.UserInputPitchVelocity = 0;
                            }
                            break;

                        case 'H':
                            if (-1 <= value && value <= 1) { CBT.UserInputPitchVelocity = value; }
                            else
                            {
                                CBT.CreateBroadcast($"Pitch value {value} is out of range. Must be between -1 and 1. Setting to 0...", STULogType.WARNING);
                                CBT.UserInputPitchVelocity = 0;
                            }
                            break;

                        case 'O':
                            if (-1 <= value && value <= 1) { CBT.UserInputRollVelocity = value; }
                            else
                            {
                                CBT.CreateBroadcast($"Roll value {value} is out of range. Must be between -1 and 1. Setting to 0...", STULogType.WARNING);
                                CBT.UserInputRollVelocity = 0;
                            }
                            break;

                        case 'Q':
                            if (-1 <= value && value <= 1) { CBT.UserInputRollVelocity = value; }
                            else
                            {
                                CBT.CreateBroadcast($"Roll value {value} is out of range. Must be between -1 and 1. Setting to 0...", STULogType.WARNING);
                                CBT.UserInputRollVelocity = 0;
                            }
                            break;

                        case 'Y':
                            if (-1 <= value && value <= 1) { CBT.UserInputYawVelocity = value; }
                            else
                            {
                                CBT.CreateBroadcast($"Yaw value {value} is out of range. Must be between -1 and 1. Setting to 0...", STULogType.WARNING);
                                CBT.UserInputYawVelocity = 0;
                            }
                            break;

                        case 'W':
                            if (-1 <= value && value <= 1) { CBT.UserInputYawVelocity = value; }
                            else
                            {
                                CBT.CreateBroadcast($"Yaw value {value} is out of range. Must be between -1 and 1. Setting to 0...", STULogType.WARNING);
                                CBT.UserInputYawVelocity = 0;
                            }
                            break;

                        default:
                            CBT.CreateBroadcast($"Command {command} is not a valid direction qualifier. Skipping...", STULogType.WARNING);
                            break;
                    }
                } 
                return true;
            }
            else
            {
                CBT.CreateBroadcast($"Could not parse command string:\n{arg}\nwhich was passed to ParseCommand(). Checking whether a special command word was used...", STULogType.WARNING);
                return false;
            }   
        }
    }
}
