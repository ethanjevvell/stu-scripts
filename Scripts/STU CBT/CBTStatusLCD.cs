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
        public partial class CBTStatusLCD : STUDisplay
        {
            public static Action<string> echo;

            public CBTStatusLCD(Action<string> Echo, IMyTerminalBlock block, int displayIndex, string font = "Monospace", float fontSize = 1) : base(block, displayIndex, font, fontSize)
            {
                echo = Echo;
            }

            public void SetupDrawSurface(IMyTextSurface surface, bool enabled)
            {
                if (enabled)
                {
                    // Draw background color
                    //  surface.ScriptBackgroundColor = new Color(0, 121, 0, 255);
                    // Set content type
                    surface.ContentType = ContentType.SCRIPT;
                    // Set script to none
                    surface.Script = "";
                }
                else
                {
                    // Draw background color
                    //  surface.ScriptBackgroundColor = new Color(106, 0, 0, 255);
                    // Set content type
                    surface.ContentType = ContentType.SCRIPT;
                    // Set script to none
                    surface.Script = "";
                }
            }

            // Poll hardware for statuses

            // conversion helpers from hardware output to sprite

            // Draw status sprites

        }
    }
}
