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
