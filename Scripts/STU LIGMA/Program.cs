using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        private const string LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL = "LIGMA_MISSION_CONTROL";
        private const string LIGMA_VEHICLE_BROADCASTER_CHANNEL = "LIGMA_VEHICLE_CONTROL";

        private string command = "";

        LIGMA missile;
        MissileReadout display;
        STUMasterLogBroadcaster broadcaster;
        IMyBroadcastListener listener;
        Phase phase;
        LIGMA.FlightPhase flightPhase;

        enum Phase {
            Idle,
            Launch,
            Flight,
            Terminal,
            Impact
        }

        Vector3D target = new Vector3D(-715.43, 23248.70, 56530.42);

        public Program() {
            broadcaster = new STUMasterLogBroadcaster(LIGMA_VEHICLE_BROADCASTER_CHANNEL, IGC, TransmissionDistance.AntennaRelay);
            listener = IGC.RegisterBroadcastListener(LIGMA_MISSION_CONTROL_BROADCASTER_CHANNEL);
            missile = new LIGMA(broadcaster, GridTerminalSystem, Me, Runtime);
            display = new MissileReadout(Me, 0, missile);
            phase = Phase.Idle;

            flightPhase = new LIGMA.FlightPhase(LIGMA.FlightController.CurrentPosition, target, Echo);

            // Script updates every 100 ticks (roughly 1.67 seconds)
            // IMPORTANT: This must match Mission Control update frequency
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument) {

            if (!string.IsNullOrEmpty(argument)) {
                if (argument == "DETONATE") {
                    LIGMA.SelfDestruct();
                } else if (argument == "LAUNCH") {
                    phase = Phase.Launch;
                } else {
                    throw new Exception("Invalid command");
                }
            }

            if (listener.HasPendingMessage) {
                var message = listener.AcceptMessage();
                if (message.Data.ToString() == "DETONATE") {
                    LIGMA.SelfDestruct();
                } else {
                    command = message.Data.ToString();
                }
            }


            LIGMA.UpdateMeasurements();
            LIGMA.PingMissionControl();

            switch (phase) {

                case Phase.Idle:
                    if (command == "LAUNCH") {
                        phase = Phase.Launch;
                    }
                    break;

                case Phase.Launch:
                    var finishedLaunch = LIGMA.Launch.Run();
                    if (finishedLaunch) {
                        phase = Phase.Flight;
                        broadcaster.Log(new STULog {
                            Sender = LIGMA.MissileName,
                            Message = "Entering flight phase",
                            Type = STULogType.WARNING,
                            Metadata = LIGMA.GetTelemetryDictionary()
                        });
                    };
                    break;

                case Phase.Flight:
                    var finishedFlight = flightPhase.Run();
                    if (finishedFlight) {
                        phase = Phase.Terminal;
                        broadcaster.Log(new STULog {
                            Sender = LIGMA.MissileName,
                            Message = "Entering terminal phase",
                            Type = STULogType.WARNING,
                            Metadata = LIGMA.GetTelemetryDictionary()
                        });
                    };
                    break;

                case Phase.Terminal:
                    LIGMA.FlightController.SetStableForwardVelocity(300);
                    LIGMA.FlightController.OrientShip(target);
                    if (Vector3D.Distance(LIGMA.FlightController.CurrentPosition, target) < 50) {
                        LIGMA.SelfDestruct();
                    }
                    break;

                case Phase.Impact:
                    // TODO
                    break;

            }

        }
    }
}
