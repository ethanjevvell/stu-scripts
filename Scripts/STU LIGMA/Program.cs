using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL = "LIGMA_MISSION_CONTROL";
        private const string LIGMA_VEHICLE_BROADCASTER_CHANNEL = "LIGMA_VEHICLE_CONTROL";
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

        MyCommandLine commandLine = new MyCommandLine();

        LIGMA Missile;
        MissileReadout Display;
        STUMasterLogBroadcaster Broadcaster;
        IMyBroadcastListener Listener;

        Phase MainPhase;
        MissileMode Mode;

        LIGMA.IFlightPlan MainFlightPlan;
        LIGMA.ILaunchPlan MainLaunchPlan;

        enum Phase {
            Idle,
            Launch,
            Flight,
            Terminal,
            Impact
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
            Broadcaster = new STUMasterLogBroadcaster(LIGMA_VEHICLE_BROADCASTER_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            Listener = IGC.RegisterBroadcastListener(LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL);
            Missile = new LIGMA(Broadcaster, GridTerminalSystem, Me, Runtime);
            Display = new MissileReadout(Me, 0, Missile);
            MainPhase = Phase.Idle;

            // Script updates every 100 ticks (roughly 1.67 seconds)
            // IMPORTANT: This must match Mission Control update frequency
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        void Main(string argument) {

            if (Listener.HasPendingMessage) {

                var message = Listener.AcceptMessage();
                var command = message.Data.ToString();

                if (command == "DETONATE") {
                    LIGMA.SelfDestruct();
                }

                if (!ALREADY_RAN_FIRST_COMMAND) {
                    if (commandLine.TryParse(command)) {
                        Broadcaster.Log(new STULog {
                            Sender = LIGMA.MissileName,
                            Message = $"Received command: {command}",
                            Type = STULogType.WARNING,
                            Metadata = LIGMA.GetTelemetryDictionary()
                        });
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
                    var finishedLaunch = MainLaunchPlan.Run();
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
                        LIGMA.ArmWarheads();
                    };
                    break;

                case Phase.Terminal:
                    LIGMA.FlightController.SetStableForwardVelocity(150);
                    LIGMA.FlightController.OrientShip(LIGMA.TargetCoordinates);
                    if (Vector3D.Distance(LIGMA.FlightController.CurrentPosition, LIGMA.TargetCoordinates) < 30) {
                        LIGMA.SelfDestruct();
                    }
                    break;

                case Phase.Impact:
                    // TODO
                    break;

            }

        }

        public void FirstRunTasks(string argument) {

            var testing = argument == "-test";
            // If using test switch, bypass all first run tasks
            if (!testing) {
                ParseTargetCoordinates(argument);
                DeduceFlightMode(argument);
            } else {
                Mode = MissileMode.Testing;
            }

            switch (Mode) {

                case MissileMode.Intraplanetary:
                    MainLaunchPlan = new LIGMA.IntraplanetaryLaunchPlan();
                    MainFlightPlan = new LIGMA.IntraplanetaryFlightPlan(LIGMA.FlightController.CurrentPosition, LIGMA.TargetCoordinates);
                    break;

                case MissileMode.PlanetToSpace:
                    LIGMA.CreateFatalErrorBroadcast("Planet to space flight not yet implemented");
                    break;

                case MissileMode.SpaceToPlanet:
                    LIGMA.CreateFatalErrorBroadcast("Space to planet flight not yet implemented");
                    break;

                case MissileMode.SpaceToSpace:
                    MainLaunchPlan = new LIGMA.SpaceToSpaceLaunchPlan();
                    MainFlightPlan = new LIGMA.SpaceToSpaceFlightPlan(LIGMA.FlightController.CurrentPosition, LIGMA.TargetCoordinates);
                    break;

                case MissileMode.Interplanetary:
                    LIGMA.CreateFatalErrorBroadcast("Interplanetary flight not yet implemented");
                    break;

                case MissileMode.Testing:
                    Broadcaster.Log(new STULog {
                        Sender = LIGMA.MissileName,
                        Message = "Entering testing mode",
                        Type = STULogType.WARNING,
                        Metadata = LIGMA.GetTelemetryDictionary()
                    });
                    MainLaunchPlan = new LIGMA.TestSuite();
                    break;

                default:
                    LIGMA.CreateFatalErrorBroadcast("Invalid flight mode in FirstRunTasks");
                    break;

            }

        }

        public void DeduceFlightMode(string argument) {

            Planet? launchPos = GetPlanetOfPoint(LIGMA.FlightController.CurrentPosition);
            Planet? targetPos = GetPlanetOfPoint(LIGMA.TargetCoordinates);

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

        public void ParseTargetCoordinates(string argument) {

            if (commandLine.ArgumentCount != 1) {
                LIGMA.CreateFatalErrorBroadcast("Only accepts on argument, which is \"X Y Z\" with no commas");
            }

            var coordStrings = commandLine.Argument(0).Split(' ');
            double x = ParseCoordinate(coordStrings[0]);
            double y = ParseCoordinate(coordStrings[1]);
            double z = ParseCoordinate(coordStrings[2]);

            LIGMA.TargetCoordinates = new Vector3D(x, y, z);
        }

        public double ParseCoordinate(string doubleString) {
            try {
                return double.Parse(doubleString);
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
    }
}
