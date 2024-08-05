using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBTLogLCD : STUDisplay
        {
            public Queue<STULog> FlightLogs;
            public static Action<string> echo;

            public CBTLogLCD(Action<string> Echo, IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize)
            {
                echo = Echo;
                FlightLogs = new Queue<STULog>();
            }
        }
    }
}
