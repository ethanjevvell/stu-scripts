using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL = "LIGMA_MISSION_CONTROL";
        private const string LIGMA_VEHICLE_BROADCASTER_CHANNEL = "LIGMA_VEHICLE_CONTROL";
        public bool ALREADY_RAN_FIRST_COMMAND = false;

        MyCommandLine commandLine = new MyCommandLine();

        LIGMA Missile;
        MissileReadout Display;
        STUMasterLogBroadcaster Broadcaster;
        IMyBroadcastListener Listener;
        Phase MainPhase;
        FlightPlan MainFlightPlan;

        enum Phase {
            Idle,
            Launch,
            Flight,
            Terminal,
            Impact
        }

        enum Mode {
            Intraplanetary,
            PlanetToSpace,
            SpaceToPlanet,
            SpaceToSpace,
            Interplanetary
        }

        Vector3D Target;

        public Program() {
            Broadcaster = new STUMasterLogBroadcaster(LIGMA_VEHICLE_BROADCASTER_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            Listener = IGC.RegisterBroadcastListener(LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL);
            Missile = new LIGMA(Broadcaster, GridTerminalSystem, Me, Runtime);
            Display = new MissileReadout(Me, 0, Missile);
            MainPhase = Phase.Idle;

            // Script updates every 100 ticks (roughly 1.67 seconds)
            // IMPORTANT: This must match Mission Control update frequency
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {

            if (Listener.HasPendingMessage) {
                var message = Listener.AcceptMessage();
                var command = message.Data.ToString();
                if (command == "DETONATE") {
                    LIGMA.SelfDestruct();
                }

                if (!ALREADY_RAN_FIRST_COMMAND) {
                    if (commandLine.TryParse(command)) {
                        FirstRunTasks(command);
                        MainPhase = Phase.Launch;
                        ALREADY_RAN_FIRST_COMMAND = true;
                    }
                }
            }

            LIGMA.UpdateMeasurements();
            LIGMA.PingMissionControl();

            switch (MainPhase) {

                case Phase.Idle:
                    break;

                case Phase.Launch:
                    var finishedLaunch = LIGMA.Launch.Run();
                    if (finishedLaunch) {
                        MainPhase = Phase.Flight;
                        Broadcaster.Log(new STULog {
                            Sender = LIGMA.MissileName,
                            Message = "Entering flight phase",
                            Type = STULogType.WARNING,
                            Metadata = LIGMA.GetTelemetryDictionary()
                        });
                    };
                    break;

                case Phase.Flight:
                    var finishedFlight = MainFlightPlan.Run();
                    if (finishedFlight) {
                        MainPhase = Phase.Terminal;
                        Broadcaster.Log(new STULog {
                            Sender = LIGMA.MissileName,
                            Message = "Entering terminal phase",
                            Type = STULogType.WARNING,
                            Metadata = LIGMA.GetTelemetryDictionary()
                        });
                    };
                    break;

                case Phase.Terminal:
                    LIGMA.FlightController.SetStableForwardVelocity(400);
                    LIGMA.FlightController.OrientShip(Target);
                    if (Vector3D.Distance(LIGMA.FlightController.CurrentPosition, Target) < 50) {
                        LIGMA.SelfDestruct();
                    }
                    break;

                case Phase.Impact:
                    // TODO
                    break;

            }

        }

        private void FirstRunTasks(string argument) {
            Mode flightMode = ParseFlightMode(argument);
            Vector3D target = ParseTargetCoordinates(argument);
            switch (flightMode) {
                case Mode.Intraplanetary:
                    MainFlightPlan = new LIGMA.IntraplanetaryFlightPlan(LIGMA.FlightController.CurrentPosition, target);
                    Target = target;
                    break;
                case Mode.PlanetToSpace:
                    throw new Exception("Planet to space flight not yet implemented");
                case Mode.SpaceToPlanet:
                    throw new Exception("Space to planet flight not yet implemented");
                case Mode.SpaceToSpace:
                    throw new Exception("Space to space flight not yet implemented");
                case Mode.Interplanetary:
                    throw new Exception("Interplanetary flight not yet implemented");
            }
        }

        private Mode ParseFlightMode(string argument) {
            MyCommandLine.SwitchCollection switches = commandLine.Switches;
            if (switches.Count > 1) {
                LIGMA.CreateErrorBroadcast("Can only specify one flight mode");
            }

            if (commandLine.Switch("intraplanetary")) {
                return Mode.Intraplanetary;
            } else if (commandLine.Switch("pts")) {
                return Mode.PlanetToSpace;
            } else if (commandLine.Switch("stp")) {
                return Mode.SpaceToPlanet;
            } else if (commandLine.Switch("sts")) {
                return Mode.SpaceToSpace;
            } else if (commandLine.Switch("interplanetary")) {
                return Mode.Interplanetary;
            } else {
                LIGMA.CreateErrorBroadcast("Invalid flight mode");
                throw new Exception("Invalid flight mode");
            }
        }

        private Vector3D ParseTargetCoordinates(string argument) {
            if (commandLine.ArgumentCount != 1) {
                LIGMA.CreateErrorBroadcast("Only accepts on argument, which is \"X Y Z\" with no commas");
            }
            var coordStrings = commandLine.Argument(0).Split(' ');
            double x = ParseCoordinate(coordStrings[0]);
            double y = ParseCoordinate(coordStrings[1]);
            double z = ParseCoordinate(coordStrings[2]);
            return new Vector3D(x, y, z);
        }

        private double ParseCoordinate(string doubleString) {
            try {
                return double.Parse(doubleString);
            } catch {
                LIGMA.CreateErrorBroadcast($"Invalid coordinate: {doubleString}");
                throw new Exception($"Invalid coordinate: {doubleString}");
            }
        }
    }
}
