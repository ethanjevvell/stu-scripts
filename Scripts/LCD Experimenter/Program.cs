
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyTerminalBlock pb;
        IMyTerminalBlock lcd;
        IMyTerminalBlock wide_lcd;

        LogLCD logLCD;

        public Program() {
            logLCD = new LogLCD(GridTerminalSystem.GetBlockWithName("LOG_LCD"), 1, "Monospace", 0.5f);
        }

        public void Main() {
            Echo("Writing...");
            logLCD.StartFrame();
            logLCD.WriteWrappableLogs(logLCD.Logs, (log) => {
                return $"{log.Sender}:: {log.Message}";
            });
            logLCD.EndAndPaintFrame();
            Echo("Wrote.");
        }

    }

}
