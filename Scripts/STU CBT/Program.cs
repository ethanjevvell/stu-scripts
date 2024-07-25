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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        

        public Program()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            /// at first, I'm going to write a framework to interact directly with the FC
            /// then I'll flesh something out like the LIGMA phases so I can abstract more flight plans
            ///

            argument = argument.Trim().ToLower();
            CBT.CreateBroadcast($"attempting to parse command: {argument}", STULogType.OK);
            if (ParseCommand(argument)) {
                // code to loop through the parsed list of individual direction requests and make calls to the FC
            };

            switch (argument)
            {
                case "stop":
                    CBT.Hover();
                    break;

                case "ac130":
                    // CBT.AC130(100, CBT.GetCurrentXCoord, CBT.GetCurrentYCoord, CBT.GetCurrentZCoord, 60);
                    break;

                case "forward":
                    CBT.FlightController.SetStableForwardVelocity(5);
                    break;

                default:
                    CBT.CreateBroadcast($"command {argument} passed to the main switch block could not be found in the cases list of special commands", STULogType.WARNING);
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



            CBT.CreateBroadcast("could not parse command passed to ParseCommand. Checking whether a special command word was used...", STULogType.WARNING);
            return false;
        }
    }
}
