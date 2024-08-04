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

        public STUMasterLogBroadcaster LogBroadcaster;
        public string LogSender = "ATC Computer";
        
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

        public const int numRunways = 16;
        public const int numDistanceLightsOfARunway = 17;
        public List<string> dockNames = new List<string>();

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
            // instantiate the LogBroadcaster
            LogBroadcaster = new STUMasterLogBroadcaster("LHQ_MASTER_LOGGER", IGC, TransmissionDistance.CurrentConstruct);

            // instantiate a dock acuator parameter struct and add it to the ship registry
            // for all known ships
            DockActuatorParameters CBT_parameters = new DockActuatorParameters();
                CBT_parameters.hinge1angle = 0F;
                CBT_parameters.pistonDistance = 10F;
                CBT_parameters.hinge2angle = -36F;
                CBT_parameters.shipDistance = 16;
                ShipRegistry.Add("CBT", CBT_parameters);

            DockActuatorParameters HBM_parameters = new DockActuatorParameters();
                HBM_parameters.hinge1angle = 27;
                HBM_parameters.pistonDistance = 4.2F;
                HBM_parameters.hinge2angle = -27;
                HBM_parameters.shipDistance = 4;
                ShipRegistry.Add("HBM", HBM_parameters);

            DockActuatorParameters B2R_parameters = new DockActuatorParameters();
                B2R_parameters.hinge1angle = 0;
                B2R_parameters.pistonDistance = 6.4F;
                B2R_parameters.hinge2angle = 18;
                B2R_parameters.shipDistance = 17;
                ShipRegistry.Add("B2R", B2R_parameters);

            DockActuatorParameters MNTS_parameters = new DockActuatorParameters();
                MNTS_parameters.hinge1angle = 36F;
                MNTS_parameters.pistonDistance = 3.1F;
                MNTS_parameters.hinge2angle = -36;
                MNTS_parameters.shipDistance = 0;
                ShipRegistry.Add("MNTS", MNTS_parameters);

            DockActuatorParameters HACKETT_parameters = new DockActuatorParameters();
                HACKETT_parameters.hinge1angle = 36;
                HACKETT_parameters.pistonDistance = 4.8F;
                HACKETT_parameters.hinge2angle = -36;
                HACKETT_parameters.shipDistance = 0;
                ShipRegistry.Add("HACKETT", HACKETT_parameters);

            DockActuatorParameters FATBOY_parameters = new DockActuatorParameters();
                FATBOY_parameters.hinge1angle = 36;
                FATBOY_parameters.pistonDistance = 4.8F;
                FATBOY_parameters.hinge2angle = -36;
                FATBOY_parameters.shipDistance = 0;
                ShipRegistry.Add("FATBOY", FATBOY_parameters);


            DockActuatorParameters grabItems_parameters = new DockActuatorParameters();
                grabItems_parameters.hinge1angle = 90;
                grabItems_parameters.pistonDistance = 0;
                grabItems_parameters.hinge2angle = -90;
                grabItems_parameters.shipDistance = 0;
                ShipRegistry.Add("grabitems", grabItems_parameters);

            DockActuatorParameters reset_parameters = new DockActuatorParameters();
                reset_parameters.hinge1angle = -90F;
                reset_parameters.pistonDistance = 0F;
                reset_parameters.hinge2angle = -90F;
                reset_parameters.shipDistance = 0;
                ShipRegistry.Add("reset", reset_parameters);

            
            // cop-out code to get a list of dock names, sorted alphabetically
            for (int i = 1; i <= numRunways - 1; i++)
            {
                if (i <= numRunways / 2)
                {
                    dockNames.Add($"E{i}");
                }
                else
                {
                    dockNames.Add($"W{i % (numRunways / 2)}");
                }
            }
            dockNames.Add("W8");
            dockNames.Sort();

            // procedurally generate DockActuatorGroup objects by looping through dockNames
            // and grabbing the associated items from the network
            for (int i = 0; i < dockNames.Count; i++)
            {
                List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
                for (int j = 0; j < numDistanceLightsOfARunway; j++)
                {
                    string strLightNumber = (j+1).ToString();
                    if (j < 9) { strLightNumber = $"0{strLightNumber}"; };
                    lights.Add(GridTerminalSystem.GetBlockWithName($"{dockNames[i]} Dock Distance Light {strLightNumber}") as IMyInteriorLight);
                }

                IMyMotorStator hinge1 = GridTerminalSystem.GetBlockWithName($"{dockNames[i]} Dock Hinge 1") as IMyMotorStator;
                IMyPistonBase piston = GridTerminalSystem.GetBlockWithName($"{dockNames[i]} Dock Piston") as IMyPistonBase;
                IMyMotorStator hinge2 = GridTerminalSystem.GetBlockWithName($"{dockNames[i]} Dock Hinge 2") as IMyMotorStator;
                IMyShipConnector connector = GridTerminalSystem.GetBlockWithName($"{dockNames[i]} Dock Connector") as IMyShipConnector;

                if (hinge1 == null || piston == null || hinge2 == null || connector == null) { Echo($"One or more of the objects is null"); };

                DockActuatorGroups.Add(new DockActuatorGroup(
                    dockNames[i],
                    hinge1,
                    piston,
                    hinge2,
                    connector,
                    lights,
                    Echo,
                    GridTerminalSystem,
                    numDistanceLightsOfARunway)
                    ); ;

                Echo($"{DockActuatorGroups[i].Name}\n");
            }

            DockActuatorGroups.OrderBy(o => o.Name);
            
        }

        public void Main(string argument)
        {
            Echo("Main() called\n");

            Echo($"sorted list of dock actuator groups:");
            foreach (var title in DockActuatorGroups)
            {
                Echo($"{title.Name}");
            }

            argument = argument.Trim();
            // string validation, proper format should be "[SHIP],[DOCK]"
            if (System.Text.RegularExpressions.Regex.IsMatch(argument,"^.+,[WE][1-8]+$"))
            {
                // continue
            }
            else if (argument.Contains("reset") && argument.Contains("all"))
            {
                foreach (var dock in DockActuatorGroups)
                {
                    dock.Move(-90, 0, -90);
                }
            }
            else { Echo($"Invalid entry\n" +
                $"[SHIP],[DOCK]\n" +
                $"Ship registry:\n");  
                foreach (var key in ShipRegistry.Keys)
                {
                    Echo($"{key}\n");
                }
                return; }

            // string manipulation logic to extract shipID and dock from argument
            int inflectionPoint = argument.IndexOf(",", 0);
            desiredShip = argument.Substring(0,inflectionPoint); desiredShip = desiredShip.Trim();
            desiredDock = argument.Substring(inflectionPoint + 1, argument.Length - inflectionPoint - 1); desiredDock = desiredDock.Trim();

            if (desiredShip == "connected") { DockActuatorGroups[dockNames.IndexOf(desiredDock)].TurnOffRunwayLights(desiredDock, 0); }

            // logic to look up ship parameters and call Move() to dock actuator group
            if (ShipRegistry.ContainsKey(desiredShip))
            {
                Echo($"Attempting to send {desiredShip} parameters to Dock Actuator Group of terminal {desiredDock}\n");

                DockActuatorGroups[dockNames.IndexOf(desiredDock)].Move(
                    ShipRegistry[desiredShip].hinge1angle,
                    ShipRegistry[desiredShip].pistonDistance,
                    ShipRegistry[desiredShip].hinge2angle
                    );

                DockActuatorGroups[dockNames.IndexOf(desiredDock)].TurnOffRunwayLights(desiredDock, ShipRegistry[desiredShip].shipDistance);
                DockActuatorGroups[dockNames.IndexOf(desiredDock)].ActivateRunwayLight(desiredDock, ShipRegistry[desiredShip].shipDistance);
                DockActuatorGroups[dockNames.IndexOf(desiredDock)].BlinkRunwayLights(desiredDock, ShipRegistry[desiredShip].shipDistance);

                Echo ($"Sent {ShipRegistry[desiredShip].hinge1angle}, {ShipRegistry[desiredShip].pistonDistance}, {ShipRegistry[desiredShip].hinge2angle} to Dock Actuator Group {DockActuatorGroups[dockNames.IndexOf(desiredDock)]}");
            }
            else { Echo($"could not find key \"{desiredShip}\""); return; }

            Echo("end of Main()");
        }

    }
}
