using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public static AirlockControlModule ACM { get; set; }

        public Program()
        {
            ACM = new AirlockControlModule();
            ACM.LoadAirlocks(GridTerminalSystem, Me, Runtime);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        public void Main(string argument, UpdateType updateSource)
        {
            double time;
            try
            {
                time = double.Parse(argument);
            }
            catch 
            {
                time = 0f;
            }
            if (time > 0f)
            {
                foreach (var airlock in ACM.Airlocks)
                {
                    airlock.StateMachine.TimeBufferMS = time;
                }
                foreach (var soloAirlock in ACM.SoloAirlocks)
                {
                    soloAirlock.StateMachine.TimeBufferMS = time;
                }
            }
            if (argument == "info") { ACM.GetAirlocks(); }
            
            ACM.UpdateAirlocks();
        }
    }
}
