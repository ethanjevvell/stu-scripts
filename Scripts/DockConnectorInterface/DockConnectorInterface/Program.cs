using Sandbox.Game.EntityComponents;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
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
        /// <summary>
        /// program for ATC to automatically move a connector on the dock to the
        /// appropriate position and illuminate a light on the tarmac for pilots
        /// to more easily land their ships at any given terminal.
        /// 
        /// The program will take a string as an input which contains the terminal to which
        /// ATC has authorized the pilot to land, as well as some unique identifier of the
        /// ship that the pilot is flying. The program will have the ship's landing parameters
        /// hard-coded and if the program doesn't recognize the ship in the passed string,
        /// it will prompt the ATC of the error and immediately return.
        /// </summary>

        // v0.0.1 testing passing strings into the program, testing movement logic of
        // pistons and hinges, etc.

        // v0.0.2 lots of code written, testing user input parser

        // v0.1.0 program will accept an argument for an angle for the test hinge with validation
        // testing parsing of full argument, "[SHIP],[DOCK]"

        // v0.1.1 messing around with the regex

        // v0.1.2 regex solidified

        // v0.1.3 introduced DockActuatorGroup class

        // v0.1.4 checking to see how the dock distance lights actually get sorted
        // for future for-loop logic


        public STUMasterLogBroadcaster LogBroadcaster;
        public string LogSender = "ATC Computer";

        public float currentHingePosition;
        public float currentHingeVelocity;

        public float currentPistonPosition;

        public string desiredShip;
        public string desiredDock;

        List<IMyTerminalBlock> dockHinge1sRaw = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> dockPistonsRaw = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> dockHinge2sRaw = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> dockConnectorsRaw = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> dockDistanceLightsRaw = new List<IMyTerminalBlock>();

        List<string> dockHinge1s = new List<string>();
        List<string> dockPistons = new List<string>();
        List<string> dockHinge2s = new List<string>();
        List<string> dockConnectors = new List<string>();
        List<string> dockDistanceLightsOfARunway = new List<string>();
        Dictionary<string, List<string>> dockDistanceLights = new Dictionary<string, List<string>>();

        public struct DockActuatorParameters
        {
            public float hinge1angle;
            public float pistonDistance;
            public float hinge2angle;
            public int shipDistance;
        }

        public Dictionary<string, DockActuatorParameters> ShipRegistry = new Dictionary<string, DockActuatorParameters>();

        public Program()
        {
            // instantiate the LogBroadcaster
            LogBroadcaster = new STUMasterLogBroadcaster("LHQ_MASTER_LOGGER", IGC, TransmissionDistance.CurrentConstruct);

            // instantiate a dock acuator parameter struct and add it to the ship registry
            // for all known ships
            DockActuatorParameters CBT_parameters = new DockActuatorParameters();
            CBT_parameters.hinge1angle = 0;
            CBT_parameters.pistonDistance = 10;
            CBT_parameters.hinge2angle = -36;
            CBT_parameters.shipDistance = 16;
            ShipRegistry.Add("CBT", CBT_parameters);

            DockActuatorParameters HBM_parameters = new DockActuatorParameters();
            HBM_parameters.hinge1angle = 45;
            HBM_parameters.pistonDistance = 0;
            HBM_parameters.hinge2angle = -45;
            HBM_parameters.shipDistance = 0;
            ShipRegistry.Add("HBM", HBM_parameters);

            // get list of all dock hinge 1s, then get list of all dock hinge 1
            // english names (by referencing the gathered objects) and sort
            GridTerminalSystem.SearchBlocksOfName("Dock Hinge 1", dockHinge1sRaw, hinge => hinge is IMyMotorStator);
            foreach (var hinge in dockHinge1sRaw)
            {
                dockHinge1s.Add(hinge.CustomName);
            }
            dockHinge1s.Sort();

            // do the above for all dock pistons
            GridTerminalSystem.SearchBlocksOfName("Dock Piston", dockPistonsRaw, piston => piston is IMyPistonBase);
            foreach (var piston in dockPistonsRaw)
            {
                dockPistons.Add(piston.CustomName);
            }
            dockPistons.Sort();

            // do the above for all dock hinge 2s
            GridTerminalSystem.SearchBlocksOfName("Dock Hinge 2", dockHinge2sRaw, hinge => hinge is IMyMotorStator);
            foreach (var hinge in dockHinge2sRaw)
            {
                dockHinge2s.Add(hinge.CustomName);
            }
            dockHinge2s.Sort();

            // do the above for all dock connectors
            GridTerminalSystem.SearchBlocksOfName("Dock Connector", dockConnectorsRaw, connector => connector is IMyShipConnector);
            foreach (var connector in dockConnectorsRaw)
            {
                dockConnectors.Add(connector.CustomName);
            }
            dockConnectors.Sort();

            // do the above for all runway distance lights
            GridTerminalSystem.SearchBlocksOfName("Dock Distance Light", dockDistanceLightsRaw, light => light is IMyInteriorLight);
            dockDistanceLightsRaw = dockDistanceLightsRaw.OrderBy(o => o.CustomName).ToList();

            foreach (var light in dockDistanceLightsRaw)
            {
                Echo($"{light.CustomName}");
            }
        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            argument = argument.Trim();
            // string validation, proper format should be "[SHIP],[DOCK]"
            if (System.Text.RegularExpressions.Regex.IsMatch(argument,"^.+,[WE][1-8]+$"))
            {
                //continue
            }
            else { Echo("invalid entry");  return; }

            // string manipulation logic to extract shipID and dock from argument
            int inflectionPoint = argument.IndexOf(",", 0);
            desiredShip = argument.Substring(0,inflectionPoint);
            desiredDock = argument.Substring(inflectionPoint + 1, argument.Length - inflectionPoint - 1);

            // logic to look up ship parameters and pass to dock actuator group control program
            if (ShipRegistry.ContainsKey(desiredShip))
            {
                //continue
            }
            else { Echo($"could not find key \"{desiredShip}\""); return; }




        }

        

        
    }
}
