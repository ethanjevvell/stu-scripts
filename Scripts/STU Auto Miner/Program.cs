using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {

    partial class Program : MyGridProgram {

        const string MINER_LOGGING_CHANNEL = "STU_AUTO_MINER_LOGGING_CHANNEL";

        static string MinerName;

        class MinerState {
            public const string INITIALIZE = "INITIALIZE";
            public const string IDLE = "IDLE";
            public const string FLY_TO_JOB_SITE = "FLY_TO_JOB_SITE";
            public const string REFUELING = "REFUELING";
            public const string MINING = "MINING";
            public const string RTB = "RTB";
            public const string HARD_FAILURE = "HARD_FAILURE";
        }

        static STUMasterLogBroadcaster LogBroadcaster { get; set; }
        STUFlightController FlightController { get; set; }
        IMyRemoteControl RemoteControl { get; set; }
        IMyShipConnector Connector { get; set; }

        Vector3 JobSite { get; set; }
        Vector3 HomeBase { get; set; }
        Dictionary<string, Action> Commands { get; set; }
        string MinerMainState { get; set; }

        List<IMyGasTank> HydrogenTanks { get; set; }
        List<IMyBatteryBlock> Batteries { get; set; }

        Queue<STULog> FlightLogs { get; set; }

        static LogLCD LogScreen { get; set; }

        // Subroutine declarations
        FlyToJobSite FlyToJobSiteStateMachine { get; set; }

        public Program() {
            HydrogenTanks = new List<IMyGasTank>();
            Batteries = new List<IMyBatteryBlock>();
            MinerName = Me.CustomData;
            if (MinerName.Length == 0) {
                CreateFatalErrorBroadcast("Miner name not set in custom data");
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            MinerMainState = MinerState.INITIALIZE;
            RemoteControl = GridTerminalSystem.GetBlockWithName("FC Remote Control") as IMyRemoteControl;
            Connector = GridTerminalSystem.GetBlockWithName("Main Connector") as IMyShipConnector;
            GridTerminalSystem.GetBlocksOfType(new List<IMyGasTank>(HydrogenTanks));
            GridTerminalSystem.GetBlocksOfType(new List<IMyBatteryBlock>(Batteries));
            FlightController = new STUFlightController(GridTerminalSystem, RemoteControl, Me);
            LogBroadcaster = new STUMasterLogBroadcaster(MINER_LOGGING_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            LogScreen = new LogLCD(GridTerminalSystem.GetBlockWithName("LogLCD"), 0, "Monospace", 0.7f);
            Commands = new Dictionary<string, Action> {
                // commands go here
            };
        }

        public void Main(string command) {

            try {
                FlightController.UpdateState();

                // Logging
                LogScreen.StartFrame();
                if (STUFlightController.FlightLogs.Count > 0) {
                    while (STUFlightController.FlightLogs.Count > 0) {
                        LogScreen.FlightLogs.Enqueue(STUFlightController.FlightLogs.Dequeue());
                    }
                }

                LogScreen.WriteWrappableLogs(LogScreen.FlightLogs);
                LogScreen.EndAndPaintFrame();

                Action commandAction = ParseCommand(command);
                if (commandAction != null) {
                    commandAction();
                }

                switch (MinerMainState) {

                    case MinerState.INITIALIZE:
                        InitializeMiner();
                        MinerMainState = MinerState.IDLE;
                        CreateOkBroadcast("Miner initialized");
                        break;

                    case MinerState.IDLE:
                        MinerMainState = MinerState.FLY_TO_JOB_SITE;
                        Vector3 testJobSite = new Vector3(-37990, -39137, -28245);
                        FlyToJobSiteStateMachine = new FlyToJobSite(FlightController, Connector, HydrogenTanks, Batteries, testJobSite, 30, 5);
                        CreateOkBroadcast("Job site set; moving to FLY_TO_JOB_SITE");
                        break;

                    case MinerState.FLY_TO_JOB_SITE:
                        FlyToJobSiteStateMachine.ExecuteStateMachine();
                        break;

                }

            } catch (Exception e) {
                CreateFatalErrorBroadcast(e.Message);
            }
        }

        Action ParseCommand(string command) {
            if (Commands.ContainsKey(command)) {
                return Commands[command];
            }
            return null;
        }

        bool SetJobSite() {
            // todo
            return true;
        }

        bool Stop() {
            // todo
            return true;
        }

        void InitializeMiner() {
            //if (!Connector.IsConnected) {
            //    CreateFatalErrorBroadcast("Miner not connected to base; exiting");
            //}
            //// Establish the home base
            //HomeBase = Connector.GetPosition();
            //// For now, just get the first character of the random entityid; would be cool to have a name generator
            //// Turn all tanks to stockpile
            //HydrogenTanks.ForEach(tank => tank.Stockpile = true);
            //// Put all batteries in recharge mode
            //Batteries.ForEach(battery => battery.ChargeMode = ChargeMode.Auto);
        }

        // Broadcast utilities
        #region
        static void CreateBroadcast(string message, string type) {
            LogBroadcaster.Log(new STULog() {
                Message = message,
                Type = type,
                Sender = MinerName
            });
            LogScreen.FlightLogs.Enqueue(new STULog() {
                Message = message,
                Type = type,
                Sender = MinerName
            });
        }

        static void CreateOkBroadcast(string message) {
            CreateBroadcast(message, STULogType.OK);
        }

        static void CreateWarningBroadcast(string message) {
            CreateBroadcast(message, STULogType.WARNING);
        }

        static void CreateErrorBroadcast(string message) {
            CreateBroadcast(message, STULogType.ERROR);
        }

        static void CreateInfoBroadcast(string message) {
            CreateBroadcast(message, STULogType.INFO);
        }

        static void CreateFatalErrorBroadcast(string message) {
            CreateBroadcast(message, STULogType.ERROR);
            LogScreen.WriteWrappableLogs(LogScreen.FlightLogs);
            LogScreen.EndAndPaintFrame();
            throw new Exception(message);
        }
        #endregion

    }

}
