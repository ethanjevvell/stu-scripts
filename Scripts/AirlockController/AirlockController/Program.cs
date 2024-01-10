using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
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
        // Finite state machine to automatically controll airlocks
        // on the LHQ base. Future versions will be portable to 
        // an arbitrary station, given that the programmer defines
        // which doors are airlocks and which doors are related to
        // each other.

        // v0.1 initial release

        // v0.2 added logic that wait to ensure doors are fully closed
        // in transition before opening the next door.
        
        // v0.2.1 changed the delay for the player to enter/exit the 
        // airlock from 3 seconds to "instantaneously" and rely on the 
        // fact that the script only runs in-game every 100 ticks (~1.4 seconds).

        // v0.2.2 changed the update interval to 10 ticks, reinstated the 
        // delay stages at 1500ms each, with the caveat that the faster-opening door
        // stays open for 1500ms longer whenever it comes up in the script for 
        // ease-of-use for the player character.

        // v0.3 testing an AirlockFSM class and having the main loop enumerate
        // across all such objects

        // v0.3.1 adding verbosity to see whether grid terminal system is pulling
        // the doors in the expected order

        // v0.3.2 added logic to ensure the airlocks are being sorted before being
        // tied to each other when they are passed as arguments to each new instance of
        // the AirlockFSM object

        // v0.3.3 removed some verbosity, cleaned up unused variables

        // v0.3.4 implemented logging

        // v0.3.5 improved logging, changed player character delay from 1500 to 1000 ms

        List<AirlockFSM> StateMachines = new List<AirlockFSM>();

        List<IMyTerminalBlock> airlocks = new List<IMyTerminalBlock>();

        List<string> airlockNames = new List<string>();

        public STUMasterLogBroadcaster LogBroadcaster;

        public string LogSender = "Airlock Controller";


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            // instantiate the LogBroadcaster
            LogBroadcaster = new STUMasterLogBroadcaster("LHQ_MASTER_LOGGER", IGC, TransmissionDistance.CurrentConstruct);

            // gather all airlocks on the base and put them into the airlocks list
            GridTerminalSystem.SearchBlocksOfName("Dock Airlock", airlocks, airlock => airlock is IMyDoor && airlock.CubeGrid == Me.CubeGrid);

            // populate the airlockNames list with the names of each airlock
            foreach (var airlock in airlocks)
            {
                airlockNames.Add(airlock.CustomName);
            }

            // sort the custom name list
            airlockNames.Sort();

            // parity check on the raw generated airlocks list
            if (airlocks.Count % 2 != 0)
            {
                Echo("unexpected parity in generating airlock list");
                return;
            }

            // create individual AirlockFSM objects by passing ascending pairs of doors
            // into a new AirlockFSM object
            for (int i = 0; i < airlockNames.Count; i += 2)
            {
                string statusMsg = $"Adding doors at indexes {i} and {i + 1}:\n{airlockNames[i]} & {airlockNames[i + 1]}\n";
                Echo(statusMsg);
                LogBroadcaster.Log(new STULog
                {
                    Sender = LogSender,
                    Message = statusMsg,
                    Type = STULogType.OK
                });

                StateMachines.Add(new AirlockFSM(GridTerminalSystem.GetBlockWithName(airlockNames[i]) as IMyDoor, GridTerminalSystem.GetBlockWithName(airlockNames[i+1]) as IMyDoor, Echo, Runtime, IGC));
            }

            // custom add pairs of doors
            StateMachines.Add(new AirlockFSM(GridTerminalSystem.GetBlockWithName("MatProc Roof Airlock A") as IMyDoor, GridTerminalSystem.GetBlockWithName("MatProc Roof Airlock B") as IMyDoor, Echo, Runtime, IGC));
            StateMachines.Add(new AirlockFSM(GridTerminalSystem.GetBlockWithName("LHQ Airlock 0A") as IMyDoor, GridTerminalSystem.GetBlockWithName("LHQ Airlock 0B") as IMyDoor, Echo, Runtime, IGC));
            StateMachines.Add(new AirlockFSM(GridTerminalSystem.GetBlockWithName("BALLS Airlock A") as IMyDoor, GridTerminalSystem.GetBlockWithName("BALLS Airlock B") as IMyDoor, Echo, Runtime, IGC));
        }

        public void Main(string argument, UpdateType updateSource)
        {
            foreach (AirlockFSM airlock in StateMachines) {
                airlock.Update();
            }
            
        }

        

    }
}
