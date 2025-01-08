using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        BALLS BALLSLauncher { get; set; }
        STUMasterLogBroadcaster Broadcaster { get; set; }
        IMyBroadcastListener Listener { get; set; }
        STUInventoryEnumerator InventoryEnumerator { get; set; }
        MyCommandLine CommandLineParser { get; set; } = new MyCommandLine();
        MyCommandLine WirelessMessageParser { get; set; } = new MyCommandLine();
        Queue<STUStateMachine> ManeuverQueue { get; set; } = new Queue<STUStateMachine>();
        STUStateMachine CurrentManeuver { get; set; }
        public Program()
        {
            Broadcaster = new STUMasterLogBroadcaster(LIGMA_VARIABLES.LIGMA_HQ_NAME, IGC, TransmissionDistance.AntennaRelay);
            Listener = IGC.RegisterBroadcastListener(LIGMA_VARIABLES.LIGMA_HQ_NAME);
            InventoryEnumerator = new STUInventoryEnumerator(GridTerminalSystem, Me);
            BALLSLauncher = new BALLS(Echo, Broadcaster, InventoryEnumerator, GridTerminalSystem, Me, Runtime);
        }
        
        public void Main(string argument, UpdateType updateSource)
        {
            
        }
    }
}
