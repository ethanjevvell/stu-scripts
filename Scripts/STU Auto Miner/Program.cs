using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {

    partial class Program : MyGridProgram {

        const string MINER_LOGGING_CHANNEL = "STU_AUTO_MINER_LOGGING_CHANNEL";

        string minerName;

        class MinerState {
            public const string INITIALIZE = "INITIALIZE";
            public const string IDLE = "IDLE";
            public const string REFUELING = "REFUELING";
            public const string MINING = "MINING";
            public const string RTB = "RTB";
            public const string HARD_FAILURE = "HARD_FAILURE";
        }

        STUFlightController flightController;
        STUMasterLogBroadcaster logBroadcaster;
        IMyRemoteControl remoteControl;
        IMyShipConnector connector;

        Vector3 jobSite;
        Vector3 homeBase;
        Dictionary<string, Action> commands;
        string minerMainState;

        // Getters and setters
        #region
        STUFlightController FlightController {
            get {
                return flightController;
            }
            set {
                flightController = value;
            }
        }

        STUMasterLogBroadcaster LogBroadcaster {
            get {
                return logBroadcaster;
            }
            set {
                logBroadcaster = value;
            }
        }

        IMyRemoteControl RemoteControl {
            get {
                return remoteControl;
            }
            set {
                remoteControl = value;
            }
        }
        Vector3 JobSite {
            get {
                return jobSite;
            }
            set {
                jobSite = value;
            }
        }
        Vector3 HomeBase {
            get {
                return homeBase;
            }
            set {
                homeBase = value;
            }
        }
        Dictionary<string, Action> Commands {
            get {
                return commands;
            }
            set {
                commands = value;
            }
        }
        string MinerMainState {
            get {
                return minerMainState;
            }
            set {
                minerMainState = value;
            }
        }
        string MinerName {
            get {
                return minerName;
            }
            set {
                minerName = value;
            }
        }
        #endregion

        public Program() {
            // For now, just get the first character of the random entityid; would be cool to have a name generator
            MinerName = Me.CustomData;
            if (MinerName.Length == 0) {
                throw new Exception("Miner name not set in custom data");
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            MinerMainState = MinerState.INITIALIZE;
            RemoteControl = GridTerminalSystem.GetBlockWithName("FC Remote Control") as IMyRemoteControl;
            FlightController = new STUFlightController(GridTerminalSystem, RemoteControl, Me);
            LogBroadcaster = new STUMasterLogBroadcaster(MINER_LOGGING_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            Commands = new Dictionary<string, Action> {
                // commands go here
            };
        }

        public void Main(string command) {

            FlightController.UpdateState();

            Action commandAction = ParseCommand(command);
            if (commandAction != null) {
                commandAction();
            }

            switch (MinerMainState) {

                case MinerState.INITIALIZE:
                    if (InitializeMiner()) {
                        MinerMainState = MinerState.IDLE;
                    }
                    break;

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

        bool InitializeMiner() {
            // If we aren't connected to a connector, fail
            return true;
        }

        // Broadcast utilities
        #region
        void CreateBroadcast(string message, string type) {
            logBroadcaster.Log(new STULog() {
                Message = message,
                Type = type,
                Sender = Me.CustomName
            });
        }

        void CreateOkBroadcast(string message) {
            CreateBroadcast(message, STULogType.OK);
        }

        void CreateWarningBroadcast(string message) {
            CreateBroadcast(message, STULogType.WARNING);
        }

        void CreateErrorBroadcast(string message) {
            CreateBroadcast(message, STULogType.ERROR);
        }

        void CreateInfoBroadcast(string message) {
            CreateBroadcast(message, STULogType.INFO);
        }

        void CreateFatalErrorBroadcast(string message) {
            CreateBroadcast(message, STULogType.ERROR);
            throw new Exception(message);
        }
        #endregion

    }

}
