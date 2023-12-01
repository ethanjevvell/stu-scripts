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

        // v0.1.5 checking some wonky for-loop stuff

        // v0.1.6 holy shit if this works

        // v0.1.7 adding verbosity


        public STUMasterLogBroadcaster LogBroadcaster;
        public string LogSender = "ATC Computer";

        public float currentHingePosition;
        public float currentHingeVelocity;

        public float currentPistonPosition;

        public string desiredShip;
        public string desiredDock;

        public List<IMyTerminalBlock> dockHinge1sRaw = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> dockPistonsRaw = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> dockHinge2sRaw = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> dockConnectorsRaw = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> dockDistanceLightsRaw = new List<IMyTerminalBlock>();

        public List<string> dockHinge1s = new List<string>();
        public List<string> dockPistons = new List<string>();
        public List<string> dockHinge2s = new List<string>();
        public List<string> dockConnectors = new List<string>();
        public Dictionary<string, List<string>> dockDistanceLightsDict_Names = new Dictionary<string, List<string>>();
        public Dictionary<int, List<IMyInteriorLight>> dockDistanceLightsDict_Objects = new Dictionary<int, List<IMyInteriorLight>>();

        int numRunways = 16;
        int numDistanceLightsOfARunway = 17;
        public List<string> runwayNames = new List<string>();

        public struct DockActuatorParameters
        {
            public float hinge1angle;
            public float pistonDistance;
            public float hinge2angle;
            public int shipDistance;
        }

        public Dictionary<string, DockActuatorParameters> ShipRegistry = new Dictionary<string, DockActuatorParameters>();

        public List<DockActuatorGroup> DockActuatorGroups = new List<DockActuatorGroup>();
        public List<DockActuatorGroupFSM> StateMachines = new List<DockActuatorGroupFSM>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            // instantiate the LogBroadcaster
            Echo("instantiate the log broadcaster\n");
            LogBroadcaster = new STUMasterLogBroadcaster("LHQ_MASTER_LOGGER", IGC, TransmissionDistance.CurrentConstruct);

            // instantiate a dock acuator parameter struct and add it to the ship registry
            // for all known ships
            Echo("instantiate a dock actuator parameter struct and add it to the ship registry for all known ships\n");
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
            Echo("get list of all dock hinge 1s, then get list of all dock hinge 1 english names and sort\n");
            GridTerminalSystem.SearchBlocksOfName("Dock Hinge 1", dockHinge1sRaw, hinge => hinge is IMyMotorStator);
            foreach (var hinge in dockHinge1sRaw)
            {
                dockHinge1s.Add(hinge.CustomName);
            }
            dockHinge1s.Sort();

            // do the above for all dock pistons
            Echo("do the same for all dock pistons\n");
            GridTerminalSystem.SearchBlocksOfName("Dock Piston", dockPistonsRaw, piston => piston is IMyPistonBase);
            foreach (var piston in dockPistonsRaw)
            {
                dockPistons.Add(piston.CustomName);
            }
            dockPistons.Sort();

            // do the above for all dock hinge 2s
            Echo("do the same for all dock hinge 2s\n");
            GridTerminalSystem.SearchBlocksOfName("Dock Hinge 2", dockHinge2sRaw, hinge => hinge is IMyMotorStator);
            foreach (var hinge in dockHinge2sRaw)
            {
                dockHinge2s.Add(hinge.CustomName);
            }
            dockHinge2s.Sort();

            // do the above for all dock connectors
            Echo("do the same for all dock connectors\n");
            GridTerminalSystem.SearchBlocksOfName("Dock Connector", dockConnectorsRaw, connector => connector is IMyShipConnector);
            foreach (var connector in dockConnectorsRaw)
            {
                dockConnectors.Add(connector.CustomName);
            }
            dockConnectors.Sort();

            // do the above for all runway distance lights
            Echo("do the same for all runway distance lights\n");
            GridTerminalSystem.SearchBlocksOfName("Dock Distance Light", dockDistanceLightsRaw, light => light is IMyInteriorLight);
            dockDistanceLightsRaw = dockDistanceLightsRaw.OrderBy(o => o.CustomName).ToList();

            // fucky wucky for-loop to cleverly build out the dockDistanceLights_Name dictionary
            // at the same time, add the light object to the dockDistanceLights_Objects dictionary
            Echo("fucky wucky for loop\n");
            for (int i = 0; i < numRunways; i++)
            {
                // create a new key and instantiate the list that will serve as each dictionary's value
                Echo($"\nouter layer i = {i}");
                dockDistanceLightsDict_Names.Add(dockDistanceLightsRaw[i*numRunways].CustomName.Substring(0, 2), new List<string>());
                dockDistanceLightsDict_Objects.Add(i*numRunways, new List<IMyInteriorLight>());
                Echo($"{dockDistanceLightsRaw[i * numRunways].CustomName.Substring(0, 2)}");
                for (int j = 0; j < numDistanceLightsOfARunway; j++)
                {
                    // take the name of the light from the Raw list and add it to the dictionary's list
                    Echo($"inner layer j = {j}");
                    dockDistanceLightsDict_Names[dockDistanceLightsRaw[i*numDistanceLightsOfARunway].CustomName.Substring(0, 2)].Add(dockDistanceLightsRaw[j+i].CustomName);
                    dockDistanceLightsDict_Objects[i * numDistanceLightsOfARunway].Add(dockDistanceLightsRaw[j + i] as IMyInteriorLight);
                }
            }

            // create instances of DockActuatorGroup
            Echo("create instances of dock actuator group\n");
            for (int i = 0; i < numRunways; i++)
            {
                Echo($"instance {i}");
                Echo($"hinge1: {dockHinge1s[i]}");
                Echo($"piston: {dockPistons[i]}");
                Echo($"hinge2: {dockHinge2s[i]}");
                Echo($"connector: {dockConnectors[i]}");
                DockActuatorGroups.Add(new DockActuatorGroup(
                    dockHinge1sRaw[i] as IMyMotorStator,
                    dockPistonsRaw[i] as IMyPistonBase,
                    dockHinge2sRaw[i] as IMyMotorStator,
                    dockConnectorsRaw[i] as IMyShipConnector,
                    dockDistanceLightsDict_Objects[i]));
                Echo($"{DockActuatorGroups[i].Hinge1.CustomName}");
            }

            // assign each DockActuatorGroup to a state machine
            // foreach (var item in DockActuatorGroups)
            // {
            //     StateMachines.Add(new DockActuatorGroupFSM(item,Runtime));
            // }

            // cop-out code to get a list of dock names, sorted alphabetically
            Echo("cop-out for loop for english names of stuff\n");
            for (int i = 1; i <= numRunways;  i++)
            {
                if (i <= numRunways/2)
                {
                    runwayNames.Add($"E{i}");
                }
                else
                {
                    runwayNames.Add($"W{i % (numRunways / 2)})");
                }
            }

        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Main() called\n");
            
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

            // logic to look up ship parameters and call Move() to dock actuator group
            if (ShipRegistry.ContainsKey(desiredShip))
            {
                Echo ($"Attempting to send {desiredShip} parameters to Dock Actuator Group of terminal {desiredDock}");
                
                DockActuatorGroups[runwayNames.IndexOf(desiredDock)].Move(
                    ShipRegistry[desiredShip].hinge1angle,
                    ShipRegistry[desiredShip].pistonDistance,
                    ShipRegistry[desiredShip].hinge2angle
                    );
                DockActuatorGroups[runwayNames.IndexOf(desiredDock)].IlluminateLight(dockDistanceLightsDict_Objects[runwayNames.IndexOf(desiredDock)], ShipRegistry[desiredShip].shipDistance);

                Echo ($"Sent {ShipRegistry[desiredShip].hinge1angle}, {ShipRegistry[desiredShip].pistonDistance}, {ShipRegistry[desiredShip].hinge2angle} to Dock Actuator Group {DockActuatorGroups[runwayNames.IndexOf(desiredDock)]}");
            }
            else { Echo($"could not find key \"{desiredShip}\""); return; }

            Echo("end of Main()");
        }

    }
}
