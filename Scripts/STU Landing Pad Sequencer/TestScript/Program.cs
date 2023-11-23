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
        // INSERT LEFT LIGHT NAMES VERBATIM
        string[] leftLightNames = { };
        // INSERT RIGHT LIGHT NAMES VERBATIM
        string[] rightLightNames = { };

        private List<IMyInteriorLight> LEFT_LIGHTS = new List<IMyInteriorLight>();
        private List<IMyInteriorLight> RIGHT_LIGHTS = new List<IMyInteriorLight>();

        private int LIGHT_INDEX = 0;
        private int TIME_ELAPSED = 0;
        private int SEQUENCE_DELAY = 500;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            LoadLights(leftLightNames, LEFT_LIGHTS);
            LoadLights(rightLightNames, RIGHT_LIGHTS);
        }

        public void Main(string argument)
        {
            ActivateLightsOverTime();
        }

        private void LoadLights(string[] lightNames, List<IMyInteriorLight> lightList)
        {
            lightList.Clear();
            foreach (var lightName in lightNames)
            {
                var light = GridTerminalSystem.GetBlockWithName(lightName.Trim()) as IMyInteriorLight;
                if (light != null)
                {
                    lightList.Add(light);
                }
            }
        }

        private void ActivateLightsOverTime()
        {
            TIME_ELAPSED += Runtime.TimeSinceLastRun.Milliseconds;
            if (TIME_ELAPSED > SEQUENCE_DELAY) 
            {
                TIME_ELAPSED = 0;
                ActivateNextLights();
            }
        }

        private void ActivateNextLights()
        {

            // If we've reached the end of the sequence, turn all the lights off to restart
            if (LIGHT_INDEX == LEFT_LIGHTS.Count)
            {
                LIGHT_INDEX = 0;
                TurnOffAllLights();
                return;
            }

            if (LIGHT_INDEX < LEFT_LIGHTS.Count)
            {
                LEFT_LIGHTS[LIGHT_INDEX].Enabled = true;
                RIGHT_LIGHTS[LIGHT_INDEX].Enabled = true;
                LIGHT_INDEX++;
            }
        }

        private void TurnOffAllLights()
        {
            for (int i = 0; i < LEFT_LIGHTS.Count; i++) 
            { 
                LEFT_LIGHTS[i].Enabled = false;
                RIGHT_LIGHTS[i].Enabled = false;
            }

            // reset sequence
            LIGHT_INDEX = 0;
            TIME_ELAPSED = 0;
        }
    }
 }

