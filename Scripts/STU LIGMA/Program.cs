using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        public bool ALREADY_RAN_FIRST_COMMAND = false;

        public struct Planet {
            public double Radius;
            public Vector3D Center;
        }

        public static Dictionary<string, Planet> CelestialBodies = new Dictionary<string, Planet> {
            {
                "TestEarth", new Planet {
                    Radius = 61050.39,
                    Center = new Vector3D(0, 0, 0)
                }
            },
            {
                "Luna", new Planet {
                    Radius = 9453.8439,
                    Center = new Vector3D(16400.0530046 ,  136405.82841528, -113627.17741361)
                }
            }
        };

        public Dictionary<LIGMA_VARIABLES.COMMANDS, Action> LIGMACommands = new Dictionary<LIGMA_VARIABLES.COMMANDS, Action>();

        MyCommandLine CommandLineParser = new MyCommandLine();

        LIGMA Missile;
        MissileReadout Display;
        STUMasterLogBroadcaster Broadcaster;
        IMyBroadcastListener Listener;

        Phase MainPhase;
        MissileMode Mode;

        LIGMA.IFlightPlan MainFlightPlan;
        LIGMA.ILaunchPlan MainLaunchPlan;

        STULog IncomingLog;

        enum Phase {
            Idle,
            Launch,
            Flight,
            Descent,
            Terminal
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
            Broadcaster = new STUMasterLogBroadcaster(LIGMA_VARIABLES.LIGMA_VEHICLE_BROADCASTER, IGC, TransmissionDistance.AntennaRelay);
            Listener = IGC.RegisterBroadcastListener(LIGMA_VARIABLES.LIGMA_MISSION_CONTROL_BROADCASTER);
            Missile = new LIGMA(Broadcaster, GridTerminalSystem, Me, Runtime);
            Display = new MissileReadout(Me, 0, Missile);
            MainPhase = Phase.Idle;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            LIGMACommands.Add(LIGMA_VARIABLES.COMMANDS.Launch, Launch);
            LIGMACommands.Add(LIGMA_VARIABLES.COMMANDS.Detonate, Detonate);
            LIGMACommands.Add(LIGMA_VARIABLES.COMMANDS.UpdateTargetData, HandleIncomingTargetData);
        }

        void Main(string argument) {

            if (Listener.HasPendingMessage) {
                var message = Listener.AcceptMessage();
                var command = message.Data.ToString();
                ParseIncomingCommand(command);
            }

            LIGMA.UpdateMeasurements();
            LIGMA.SendTelemetry();

            switch (MainPhase) {

                case Phase.Idle:
                    break;

                case Phase.Launch:
                    var finishedLaunch = MainLaunchPlan.Run();
                    if (finishedLaunch) {
                        MainPhase = Phase.Flight;
                        LIGMA.CreateWarningBroadcast("Entering flight phase");
                        // Stop any roll created during this phase
                        LIGMA.FlightController.SetVr(0);
                    };
                    break;

                case Phase.Flight:
                    var finishedFlight = MainFlightPlan.Run();
                    if (finishedFlight) {
                        MainPhase = Phase.Descent;
                        LIGMA.CreateWarningBroadcast("Entering descent phase");
                        LIGMA.ArmWarheads();
                        // Stop any roll created during this phase
                        LIGMA.FlightController.SetVr(0);
                    };
                    break;

                case Phase.Descent:
                    var velocityStable = LIGMA.FlightController.SetStableForwardVelocity(80);
                    LIGMA.FlightController.OptimizeShipRoll(LIGMA.TargetData.Position);
                    LIGMA.FlightController.AlignShipToTarget(LIGMA.TargetData.Position);

                    if (velocityStable) {
                        LIGMA.CreateWarningBroadcast("Entering terminal phase");
                        MainPhase = Phase.Terminal;
                        // Stop any roll created during this phase
                        LIGMA.FlightController.SetVr(0);
                    }
                    break;

                case Phase.Terminal:
                    LIGMA.FlightController.StableFreeFall();
                    LIGMA.FlightController.AlignShipToTarget(LIGMA.TargetData.Position);
                    if (Vector3D.Distance(LIGMA.FlightController.CurrentPosition, LIGMA.TargetData.Position) < 30) {
                        LIGMA.SelfDestruct();
                    }
                    break;

            }

        }

        public void ParseIncomingCommand(string logString) {

            try {
                IncomingLog = STULog.Deserialize(logString);
            } catch {
                LIGMA.CreateErrorBroadcast($"Failed to deserialize incoming log: {logString}");
                return;
            }

            string message = IncomingLog.Message;

            if (CommandLineParser.TryParse(message)) {

                int arguments = CommandLineParser.ArgumentCount;

                if (arguments != 1) {
                    LIGMA.CreateErrorBroadcast("LIGMA only accepts one argument at a time.");
                    return;
                }

                LIGMA_VARIABLES.COMMANDS commandEnum;
                string commandString = CommandLineParser.Argument(0);

                if (!Enum.TryParse(commandString, out commandEnum)) {
                    LIGMA.CreateErrorBroadcast($"Command {commandString} not found in LIGMA commands enum.");
                    return;
                }

                Action commandAction;
                if (!LIGMACommands.TryGetValue(commandEnum, out commandAction)) {
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

            switch (Mode) {

                case MissileMode.Intraplanetary:
                    MainLaunchPlan = new LIGMA.IntraplanetaryLaunchPlan();
                    MainFlightPlan = new LIGMA.IntraplanetaryFlightPlan(LIGMA.FlightController.CurrentPosition, LIGMA.TargetData.Position);
                    break;

                case MissileMode.PlanetToSpace:
                    LIGMA.CreateFatalErrorBroadcast("Planet to space flight not yet implemented");
                    break;

                case MissileMode.SpaceToPlanet:
                    LIGMA.CreateFatalErrorBroadcast("Space to planet flight not yet implemented");
                    break;

                case MissileMode.SpaceToSpace:
                    MainLaunchPlan = new LIGMA.SpaceToSpaceLaunchPlan();
                    MainFlightPlan = new LIGMA.SpaceToSpaceFlightPlan(LIGMA.FlightController.CurrentPosition, LIGMA.TargetData.Position);
                    break;

                case MissileMode.Interplanetary:
                    LIGMA.CreateFatalErrorBroadcast("Interplanetary flight not yet implemented");
                    break;

                case MissileMode.Testing:
                    LIGMA.CreateWarningBroadcast("Entering testing mode");
                    MainLaunchPlan = new LIGMA.TestSuite();
                    break;

                default:
                    LIGMA.CreateFatalErrorBroadcast("Invalid flight mode in FirstRunTasks");
                    break;

            }

        }

        public void DeduceFlightMode() {

            Planet? launchPos = GetPlanetOfPoint(LIGMA.FlightController.CurrentPosition);
            Planet? targetPos = GetPlanetOfPoint(LIGMA.TargetData.Position);

            if (OnSamePlanet(launchPos, targetPos)) {
                Mode = MissileMode.Intraplanetary;
            } else if (!InSpace(launchPos) && InSpace(targetPos)) {
                Mode = MissileMode.PlanetToSpace;
            } else if (InSpace(launchPos) && !InSpace(targetPos)) {
                Mode = MissileMode.SpaceToPlanet;
            } else if (InSpace(launchPos) && InSpace(targetPos)) {
                Mode = MissileMode.SpaceToSpace;
            } else if (OnDifferentPlanets(launchPos, targetPos)) {
                Mode = MissileMode.Interplanetary;
            } else {
                LIGMA.CreateFatalErrorBroadcast("Invalid flight mode in DeduceFlightMode");
            }

            LIGMA.LaunchPlanet = launchPos;
            LIGMA.TargetPlanet = targetPos;

        }

        public bool OnSamePlanet(Planet? launchPlanet, Planet? targetPlanet) {
            if (InSpace(launchPlanet) || InSpace(targetPlanet)) {
                return false;
            }
            return launchPlanet.Equals(targetPlanet);
        }

        public bool OnDifferentPlanets(Planet? launchPlanet, Planet? targetPlanet) {
            if (InSpace(launchPlanet) || InSpace(targetPlanet)) {
                return false;
            }
            return !launchPlanet.Equals(targetPlanet);
        }

        public bool InSpace(Planet? planet) {
            return planet == null;
        }

        public Planet? GetPlanetOfPoint(Vector3D point) {
            var detectionBuffer = 1000;
            foreach (var body in CelestialBodies.Keys) {
                Planet planet = CelestialBodies[body];
                BoundingSphereD sphere = new BoundingSphereD(planet.Center, planet.Radius + detectionBuffer);
                // if the point is inside the planet's detection sphere or intersects it, it is on the planet
                if (sphere.Contains(point) == ContainmentType.Contains || sphere.Contains(point) == ContainmentType.Intersects) {
                    return planet;
                }
            }
            return null;
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
            switch (Mode) {
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
                LIGMA.TargetData = STURaycaster.DeserializeHitInfo(IncomingLog.Metadata);
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
            if (ALREADY_RAN_FIRST_COMMAND) {
                LIGMA.CreateErrorBroadcast("Cannot launch more than once");
                return;
            }

            if (LIGMA.TargetData.Position == default(Vector3D)) {
                LIGMA.CreateErrorBroadcast("Cannot launch without target coordinates");
                return;
            }

            ALREADY_RAN_FIRST_COMMAND = true;
            FirstRunTasks();
            MainPhase = Phase.Launch;
            // Insurance in case LIGMA was modified on launch pad
            LIGMA.FlightController.UpdateShipMass();
        }

        public void Detonate() {
            LIGMA.SelfDestruct();
        }
    }
}
