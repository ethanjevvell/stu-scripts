using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        bool Canary = true;

        CR ThisCR;
        STUMasterLogBroadcaster Broadcaster;
        IMyBroadcastListener Listener;
        MyCommandLine WirelessMessageParser { get; set; } = new MyCommandLine();
        MyCommandLine CommandLineParser { get; set; } = new MyCommandLine();
        TEA Modem = new TEA();

        public Program() {
            Broadcaster = new STUMasterLogBroadcaster(CBT_VARIABLES.CBT_BROADCAST_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            Listener = IGC.RegisterBroadcastListener(CBT_VARIABLES.CBT_BROADCAST_CHANNEL);
            ThisCR = new CR(Echo, Broadcaster, GridTerminalSystem, Me, Runtime);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource) {
            try 
            {
                HandleWirelessMessages();

                argument = argument.Trim().ToUpper();
                
                if (argument != "") 
                {
                    if (!CheckSpecialCommandWord(argument))
                    {
                        if (!ParseCommand(argument))
                        {
                            CR.AddToLogQueue($"cr machine broke", STULogType.ERROR);
                        }
                    }
                }

                CR.DockingModule.UpdateDockingModule();
            } 
            catch (Exception e) {
                CR.AddToLogQueue($"Error: {e.Message}... {e.Source} ({e.StackTrace})", STULogType.ERROR);
            } 
            finally {
                CR.UpdateLogScreens();
                CR.ACM.UpdateAirlocks();
            }

        }

        public void HandleWirelessMessages()
        {
            if (Listener.HasPendingMessage)
            {
                var rawMessage = Listener.AcceptMessage();
                string message = rawMessage.Data.ToString();
                STULog incomingLog = STULog.Deserialize(message);
                // string decryptedMessage = Modem.Decrypt(incomingLog.Message, CR_VARIABLES.TEA_KEY);

                CR.AddToLogQueue($"Received message: {incomingLog.Message}", STULogType.INFO);

                if (WirelessMessageParser.TryParse(incomingLog.Message.ToUpper()))
                {
                    switch (WirelessMessageParser.Argument(0))
                    {
                        case "PING":
                            CR.AddToLogQueue($"Received PING from {incomingLog.Sender}", STULogType.INFO);
                            CR.CreateBroadcast("PONG");
                            break;
                        case "DOCK":
                            CR.AddToLogQueue($"Received DOCK request from {incomingLog.Sender}", STULogType.INFO);
                            CR.DockingModule.DockRequestReceivedFlag = true;
                            break;
                    }
                }

            }
        }

        public bool CheckSpecialCommandWord(string arg)
        {
            switch (arg)
            {
                case "PING":
                    CR.AddToLogQueue("Broadcasting PING", STULogType.INFO);
                    CR.CreateBroadcast("PING");
                    return true;
                default:
                    CR.AddToLogQueue($"String \"{arg}\" is not a special command word", STULogType.INFO);
                    return false;
            }
        }

        public bool ParseCommand(string arg)
        {
            CR.AddToLogQueue($"Parsing command string \"{arg}\"", STULogType.INFO);
            /// code to break up space-separated commands that might be entered into the terminal.
            /// e.g. "F5" should be interpreted as "move forward at 5m/s"
            /// "F5 D1" should be interpreted as "move forward 5m/s AND down 1m/s"
            /// "R5 Y-1" should be interpreted as "move right at 5m/s and yaw left at 1 degree per second"
            /// F = move forward
            /// B = move backwards
            /// U = move up
            /// D = move down
            /// R = move right
            /// L = move left
            /// P = pitch, positive number = pitch up, negative number = pitch down
            /// H = pitch down (?)
            /// O = roll, positive number = roll clockwise wrt forward axis, negative number = roll counter-clockwise wrt forward axis
            /// Q = roll counter-clockwise (?)
            /// Y = yaw, positive number = yaw right, negative number = yaw left
            /// W = yaw left

            // loop through the passed string and act on valid direction qualifiers (listed above)
            if (CommandLineParser.TryParse(arg))
            {
                for (int i = 0; i < CommandLineParser.ArgumentCount; i++)
                {
                    string command = CommandLineParser.Argument(i);
                    if (command.Length < 2)
                    {
                        CR.AddToLogQueue($"Command '{command}' is too short to be valid. Skipping...", STULogType.WARNING);
                        continue;
                    }

                    char direction = command[0];
                    string secondCharacter = command.Substring(1);
                    try
                    {
                        float.Parse(secondCharacter);
                    }
                    catch (Exception e)
                    {
                        Echo($"EXCEPTION: {e.Message} \n{e.StackTrace}");
                        CR.AddToLogQueue($"EXCEPTION: {e.Message}; {e.StackTrace}");
                        break;
                    }
                    float result;
                    float value = float.TryParse(command.Substring(1), out result) ? result : 0;

                    switch (direction)
                    {
                        
                    }
                }
                return true;
            }
            else
            {
                CR.AddToLogQueue($"damn this shit broken fr fr", STULogType.ERROR); return false;
            }
        }
    }
}
