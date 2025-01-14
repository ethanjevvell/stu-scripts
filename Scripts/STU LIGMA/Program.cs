using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        public bool ALREADY_RAN_FIRST_COMMAND = false;
        bool FINISHED_LOADING_HARDWARE = false;
        bool ALREADY_SAID_GOODBYE = false;

        public Dictionary<string, Action> _LIGMACommands = new Dictionary<string, Action>();

        MyCommandLine _commandLineParser = new MyCommandLine();
        STUInventoryEnumerator _inventoryEnumerator;

        LIGMA _missile;
        MissileReadout _display;
        static STUMasterLogBroadcaster s_telemetryBroadcaster;
        static STUMasterLogBroadcaster s_logBroadcaster;
        IMyUnicastListener _unicastListener;

        MissileMode _mode;

        LIGMA.ILaunchPlan _mainLaunchPlan;
        LIGMA.IFlightPlan _mainFlightPlan;
        LIGMA.IDescentPlan _mainDescentPlan;
        LIGMA.ITerminalPlan _mainTerminalPlan;

        STULog _tempIncomingLog;

        enum Phase {
            Idle,
            Launch,
            Flight,
            Descent,
            Terminal,
        }

        enum MissileMode {
            Intraplanetary,
            PlanetToSpace,
            SpaceToPlanet,
            SpaceToSpace,
            Interplanetary,
            Testing
        }

        public Program() {
            s_telemetryBroadcaster = new STUMasterLogBroadcaster(LIGMA_VARIABLES.LIGMA_TELEMETRY_BROADCASTER, IGC, TransmissionDistance.AntennaRelay);
            s_logBroadcaster = new STUMasterLogBroadcaster(LIGMA_VARIABLES.LIGMA_LOG_BROADCASTER, IGC, TransmissionDistance.AntennaRelay);
            _unicastListener = IGC.UnicastListener;
            _missile = new LIGMA(s_telemetryBroadcaster, s_logBroadcaster, GridTerminalSystem, Me, Runtime);
            _display = new MissileReadout(Me, 0, _missile);
            _inventoryEnumerator = new STUInventoryEnumerator(GridTerminalSystem, Me);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _LIGMACommands.Add(LIGMA_VARIABLES.COMMANDS.Launch, Launch);
            _LIGMACommands.Add(LIGMA_VARIABLES.COMMANDS.Detonate, Detonate);
            _LIGMACommands.Add(LIGMA_VARIABLES.COMMANDS.Test, Test);
            _LIGMACommands.Add(LIGMA_VARIABLES.COMMANDS.UpdateTargetData, HandleIncomingTargetData);
        }

        void Main(string argument) {

            if (!FINISHED_LOADING_HARDWARE) {
                FINISHED_LOADING_HARDWARE = _missile.LoadHardware(GridTerminalSystem);
                return;
            }

            Echo(LIGMA.CurrentPhase.ToString());

            try {

                _inventoryEnumerator.EnumerateInventories();

                if (_unicastListener.HasPendingMessage) {
                    var message = _unicastListener.AcceptMessage();
                    var command = message.Data.ToString();
                    ParseIncomingCommand(command);
                }

                LIGMA.UpdateState();

                switch (LIGMA.CurrentPhase) {

                    case LIGMA.Phase.Idle:
                        break;

                    case LIGMA.Phase.Launch:

                        var finishedLaunch = _mainLaunchPlan.Run();

                        if (finishedLaunch) {
                            LIGMA.CurrentPhase = LIGMA.Phase.Flight;
                            LIGMA.CreateWarningBroadcast("Entering flight phase");
                            // Stop any roll created during this phase
                            LIGMA.FlightController.SetVr(0);
                            if (LIGMA.IS_STAGED_LIGMA) {
                                LIGMA.JettisonLaunchStage();
                            }
                        };
                        break;

                    case LIGMA.Phase.Flight:
                        var finishedFlight = _mainFlightPlan.Run();
                        if (finishedFlight) {
                            try {
                                LIGMA.CurrentPhase = LIGMA.Phase.Descent;
                                LIGMA.CreateWarningBroadcast("Entering descent phase");
                                // Stop any roll created during this phase
                                LIGMA.FlightController.SetVr(0);
                                if (LIGMA.IS_STAGED_LIGMA) {
                                    LIGMA.JettisonFlightStage();
                                }
                            } catch (Exception e) {
                                LIGMA.CreateErrorBroadcast(e.ToString());
                            }
                        };
                        break;

                    case LIGMA.Phase.Descent:
                        var finishedDescent = _mainDescentPlan.Run();
                        if (finishedDescent) {
                            LIGMA.CurrentPhase = LIGMA.Phase.Terminal;
                            LIGMA.CreateWarningBroadcast("Entering terminal phase");
                            // Stop any roll created during this phase
                            LIGMA.FlightController.SetVr(0);
                        };
                        //
                        break;

                    case LIGMA.Phase.Terminal:
                        var finishedTerminal = _mainTerminalPlan.Run();
                        if (!ALREADY_SAID_GOODBYE && Vector3D.Distance(LIGMA.FlightController.CurrentPosition, LIGMA.TargetData.Position) < _mainTerminalPlan.TERMINAL_VELOCITY / 2) {
                            LIGMA.CreateErrorBroadcast(LIGMA_VARIABLES.COMMANDS.SendGoodbye);
                            ALREADY_SAID_GOODBYE = true;
                        }
                        // Failsafe: If LIGMA runs out of fuel, self-destruct ONLY in terminal phase
                        if (_inventoryEnumerator.GetItemTotals()["Hydrogen"] == 0) {
                            LIGMA.ArmWarheads();
                            LIGMA.SelfDestruct();
                        }
                        break;

                }

            } catch (Exception e) {
                LIGMA.CreateErrorBroadcast($"Error in main loop: {e}. LIGMA terminating program execution.");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                Echo(e.Message);
            } finally {
                LIGMA.UpdateMeasurements();
                LIGMA.SendTelemetry();
            }

        }

        public void ParseIncomingCommand(string logString) {

            try {
                _tempIncomingLog = STULog.Deserialize(logString);
            } catch {
                LIGMA.CreateErrorBroadcast($"Failed to deserialize incoming log: {logString}");
                return;
            }

            string message = _tempIncomingLog.Message;

            if (_commandLineParser.TryParse(message)) {

                int arguments = _commandLineParser.ArgumentCount;

                if (arguments != 1) {
                    LIGMA.CreateErrorBroadcast("LIGMA only accepts one argument at a time.");
                    return;
                }

                string commandString = _commandLineParser.Argument(0);
                Action commandAction;
                if (!_LIGMACommands.TryGetValue(commandString, out commandAction)) {
                    LIGMA.CreateErrorBroadcast($"Command {commandString} not found in LIGMA commands dictionary.");
                    return;
                }

                commandAction();

            } else {

                LIGMA.CreateErrorBroadcast($"Failed to parse command: {message}");

            }
        }

        public void FirstRunTasks() {

            DeduceFlightMode();

            switch (_mode) {

                case MissileMode.Intraplanetary:
                    _mainLaunchPlan = new LIGMA.IntraplanetaryLaunchPlan();
                    _mainFlightPlan = new LIGMA.IntraplanetaryFlightPlan();
                    _mainDescentPlan = new LIGMA.IntraplanetaryDescentPlan();
                    _mainTerminalPlan = new LIGMA.IntraplanetaryTerminalPlan();
                    break;

                case MissileMode.PlanetToSpace:
                    _mainLaunchPlan = new LIGMA.PlanetToSpaceLaunchPlan();
                    _mainFlightPlan = new LIGMA.PlanetToSpaceFlightPlan();
                    _mainDescentPlan = new LIGMA.PlanetToSpaceDescentPlan();
                    _mainTerminalPlan = new LIGMA.PlanetToSpaceTerminalPlan();
                    break;

                case MissileMode.SpaceToPlanet:
                    _mainLaunchPlan = new LIGMA.SpaceToPlanetLaunchPlan();
                    _mainFlightPlan = new LIGMA.SpaceToPlanetFlightPlan();
                    _mainDescentPlan = new LIGMA.SpaceToPlanetDescentPlan();
                    _mainTerminalPlan = new LIGMA.SpaceToPlanetTerminalPlan();
                    break;

                case MissileMode.SpaceToSpace:
                    _mainLaunchPlan = new LIGMA.SpaceToSpaceLaunchPlan();
                    _mainFlightPlan = new LIGMA.SpaceToSpaceFlightPlan();
                    _mainDescentPlan = new LIGMA.SpaceToSpaceDescentPlan();
                    _mainTerminalPlan = new LIGMA.SpaceToSpaceTerminalPlan();
                    break;

                case MissileMode.Interplanetary:
                    _mainLaunchPlan = new LIGMA.InterplanetaryLaunchPlan();
                    _mainFlightPlan = new LIGMA.InterplanetaryFlightPlan();
                    _mainDescentPlan = new LIGMA.InterplanetaryDescentPlan();
                    _mainTerminalPlan = new LIGMA.InterplanetaryTerminalPlan();
                    break;

                case MissileMode.Testing:
                    LIGMA.CreateWarningBroadcast("Entering testing mode");
                    _mainLaunchPlan = new LIGMA.TestSuite();
                    break;

                default:
                    LIGMA.CreateFatalErrorBroadcast("Invalid flight mode in FirstRunTasks");
                    break;

            }

        }

        public void DeduceFlightMode() {

            STUGalacticMap.Planet? launchPos = STUGalacticMap.GetPlanetOfPoint(LIGMA.FlightController.CurrentPosition, LIGMA_VARIABLES.PLANETARY_DETECTION_BUFFER);
            STUGalacticMap.Planet? targetPos = STUGalacticMap.GetPlanetOfPoint(LIGMA.TargetData.Position, LIGMA_VARIABLES.PLANETARY_DETECTION_BUFFER);

            if (OnSamePlanet(launchPos, targetPos)) {
                _mode = MissileMode.Intraplanetary;
            } else if (!InSpace(launchPos) && InSpace(targetPos)) {
                _mode = MissileMode.PlanetToSpace;
            } else if (InSpace(launchPos) && !InSpace(targetPos)) {
                _mode = MissileMode.SpaceToPlanet;
            } else if (InSpace(launchPos) && InSpace(targetPos)) {
                _mode = MissileMode.SpaceToSpace;
            } else if (OnDifferentPlanets(launchPos, targetPos)) {
                _mode = MissileMode.Interplanetary;
            } else {
                LIGMA.CreateFatalErrorBroadcast("Invalid flight mode in DeduceFlightMode");
            }

            LIGMA.LaunchPlanet = launchPos;
            LIGMA.TargetPlanet = targetPos;

        }

        public bool OnSamePlanet(STUGalacticMap.Planet? launchPlanet, STUGalacticMap.Planet? targetPlanet) {
            if (InSpace(launchPlanet) || InSpace(targetPlanet)) {
                return false;
            }
            return launchPlanet.Equals(targetPlanet);
        }

        public bool OnDifferentPlanets(STUGalacticMap.Planet? launchPlanet, STUGalacticMap.Planet? targetPlanet) {
            if (InSpace(launchPlanet) || InSpace(targetPlanet)) {
                return false;
            }
            return !launchPlanet.Equals(targetPlanet);
        }

        public bool InSpace(STUGalacticMap.Planet? planet) {
            return planet == null;
        }

        public double ParseCoordinate(string doubleString) {
            try {
                string numPortion = doubleString.Split(':')[1].Trim().ToString();
                return double.Parse(numPortion);
            } catch {
                LIGMA.CreateFatalErrorBroadcast($"Invalid coordinate: {doubleString}");
                throw new Exception($"Invalid coordinate: {doubleString}");
            }
        }

        public string GetModeString() {
            switch (_mode) {
                case MissileMode.Intraplanetary:
                    return "intraplanetary flight";
                case MissileMode.PlanetToSpace:
                    return "planet-to-space flight";
                case MissileMode.SpaceToPlanet:
                    return "space-to-planet flight";
                case MissileMode.SpaceToSpace:
                    return "space-to-space flight";
                case MissileMode.Interplanetary:
                    return "interplanetary flight";
                default:
                    return "invalid flight mode";
            }
        }

        public void HandleIncomingTargetData() {
            try {
                LIGMA.UpdateTargetData(STURaycaster.DeserializeHitInfo(_tempIncomingLog.Metadata));
                string x = LIGMA.TargetData.Position.X.ToString("0.00");
                string y = LIGMA.TargetData.Position.Y.ToString("0.00");
                string z = LIGMA.TargetData.Position.Z.ToString("0.00");
                string broadcastString = $"CONFIRMED - Target coordinates set to ({x}, {y}, {z})";
                LIGMA.CreateOkBroadcast(broadcastString);
            } catch (Exception e) {
                LIGMA.CreateErrorBroadcast($"Failed to parse target data: {e}");
            }
        }

        public void Launch() {
            LIGMA.CreateOkBroadcast("Received launch command");
            if (ALREADY_RAN_FIRST_COMMAND) {
                LIGMA.CreateErrorBroadcast("Cannot launch more than once");
                return;
            }

            if (LIGMA.TargetData.Position == default(Vector3D)) {
                LIGMA.CreateErrorBroadcast("Cannot launch without target coordinates");
                return;
            }

            try {
                ALREADY_RAN_FIRST_COMMAND = true;
                FirstRunTasks();
                LIGMA.CurrentPhase = LIGMA.Phase.Launch;
                // Insurance in case LIGMA was modified on launch pad
                LIGMA.FlightController.UpdateShipMass();
                LIGMA.CreateOkBroadcast($"Launching in {GetModeString()}");
            } catch (Exception e) {
                LIGMA.CreateFatalErrorBroadcast($"Error during launch: {e}");
            }
        }

        public void Test() {
            LIGMA.CurrentPhase = LIGMA.Phase.Launch;
            _mainLaunchPlan = new LIGMA.TestSuite();
        }

        public void Detonate() {
            LIGMA.SelfDestruct();
        }

    }
}
