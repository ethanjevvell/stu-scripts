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


        public Program()
        {
            // instantiate the actual CBT at the Program level so that all the methods in here will be directed towards a specific CBT object (the one that I fly around in game)
            Broadcaster = new STUMasterLogBroadcaster(CBT_VARIABLES.CBT_BROADCAST_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            CBTShip = new CBT(Broadcaster, GridTerminalSystem, Me, Runtime);
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            /// at first, I'm going to write a framework to interact directly with the FC
            /// then I'll flesh something out like the LIGMA phases so I can abstract more flight plans
            ///

            CBT.FlightController.UpdateState();

            argument = argument.Trim().ToLower();
            // CBT.CreateBroadcast($"attempting to parse command: {argument}", STULogType.OK);
            if (ParseCommand(argument))
            {
                // code to loop through the parsed list of individual direction requests and make calls to the FC
            };

            Func<bool> maneuver = CBT.GenericManeuver;

            switch (argument)
            {
                case "stop":
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    maneuver = CBT.Hover;
                    break;

                case "ac130":
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    // CBT.AC130(100, CBT.GetCurrentXCoord, CBT.GetCurrentYCoord, CBT.GetCurrentZCoord, 60);
                    break;

                case "forward":
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    CBT.UserInputForwardVelocity = 5;
                    maneuver = CBT.GenericManeuver;
                    break;

                default:
                    // CBT.CreateBroadcast($"command {argument} passed to the main switch block could not be found in the cases list of special commands", STULogType.WARNING);
                    CBT.CurrentPhase = CBT.Phase.Executing;
                    break;
            }

            switch (CBT.CurrentPhase)
            {
                case CBT.Phase.Idle:
                    // CBT.CreateBroadcast("CBT is idle", STULogType.OK);
                    Echo("CBT is idle");
                    break;
                case CBT.Phase.Executing:
                    Echo($"CBT is executing a flight plan {Runtime.TimeSinceLastRun.Milliseconds}");
                    // CBT.CreateBroadcast("CBT is executing a flight plan", STULogType.OK);
                    var finishedExecuting = CBT.Hover();
                    if (finishedExecuting)
                    {
                        Echo("CBT has finished executing the flight plan");
                        CBT.CurrentPhase = CBT.Phase.Idle;
                    }
                    break;
            }
        }

        public bool ParseCommand(string arg)
        {
            // code to break up space-separated commands that might be entered into the terminal.
            // e.g. "F5" should be interpreted as "move forward at 5m/s"
            // "F5 D1" should be interpreted as "move forward 5m/s AND down 1m/s"
            // "R5 Y-1" should be interpreted as "move right at 5m/s and yaw left at 1 degree per second"
            // F = move forward
            // B = move backwards
            // U = move up
            // D = move down
            // R = move right
            // L = move left
            // P = pitch, positive number = pitch up, negative number = pitch down
            // H = pitch down (?)
            // O = roll, positive number = roll clockwise wrt forward axis, negative number = roll counter-clockwise wrt forward axis
            // Q = roll counter-clockwise (?)
            // Y = yaw, positive number = yaw right, negative number = yaw left
            // W = yaw left

            // loop through the passed string and act on valid direction qualifiers (listed above)
            if (CommandLineParser.TryParse(arg))
            {
                for (int i = 0; i < CommandLineParser.ArgumentCount; i++)
                {
                    string command = CommandLineParser.Argument(i);
                    if (command.Length < 2)
                    {
                        // CBT.CreateBroadcast($"Command: {command} is too short to be valid. Skipping...", STULogType.WARNING);
                        continue;
                    }
                    else
                    {
                        // CBT.CreateBroadcast($"Command: {command} is fucked up. Aborting...", STULogType.ERROR);
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
                            CBT.UserInputPitchVelocity = value;
                            break;

                        case 'H':
                            CBT.UserInputPitchVelocity = (value) * -1;
                            break;

                        case 'O':
                            CBT.UserInputRollVelocity = value;
                            break;

                        case 'Q':
                            CBT.UserInputRollVelocity = (value) * -1;
                            break;

                        case 'Y':
                            CBT.UserInputYawVelocity = value;
                            break;

                        case 'W':
                            CBT.UserInputYawVelocity = (value) * -1;
                            break;

                        default:
                            // CBT.CreateBroadcast($"Command {command} is not a valid direction qualifier. Skipping...", STULogType.WARNING);
                            break;
                    }
                }   
            }

            // CBT.CreateBroadcast($"Could not parse command string:\n{arg}\nwhich was passed to ParseCommand(). Checking whether a special command word was used...", STULogType.WARNING);
            return false;
        }

        public void ExecuteManeuver(string maneuver)
        {
            // code to execute a maneuver based on a string passed to the method
            // e.g. "hover" should call the Hover() method in the CBT class
           
        }
    }
}
