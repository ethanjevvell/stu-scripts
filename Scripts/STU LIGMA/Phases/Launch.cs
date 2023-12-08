using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class Launch {

                private static Vector3D TestTarget = new Vector3D(484.92, 3816.29, -1834.40);

                public enum LaunchPhase {
                    Idle,
                    InitialBurn,
                    TurnBurn,
                    Flight,
                    Terminal
                }

                private static bool VzSet = false;
                private static bool VxSet = false;
                private static bool VySet = false;
                private static bool orientationSet = false;

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static void Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:

                            phase = LaunchPhase.InitialBurn;
                            Broadcaster.Log(new STULog {
                                Sender = MissileName,
                                Message = "Starting initial burn",
                                Type = STULogType.WARNING,
                                Metadata = GetTelemetryDictionary()
                            });

                            break;

                        case LaunchPhase.InitialBurn:

                            VzSet = FlightController.ControlForward(70);
                            VxSet = FlightController.ControlRight(0);
                            VySet = FlightController.ControlUp(0);

                            if (VzSet && VxSet && VySet) {
                                Broadcaster.Log(new STULog {
                                    Sender = MissileName,
                                    Message = "Entering turn phase",
                                    Type = STULogType.WARNING,
                                    Metadata = GetTelemetryDictionary()
                                });
                                phase = LaunchPhase.TurnBurn;
                            }

                            break;

                        case LaunchPhase.TurnBurn:

                            orientationSet = FlightController.OrientShip(TestTarget);
                            VzSet = FlightController.ControlForward(70);
                            VxSet = FlightController.ControlRight(0);
                            VySet = FlightController.ControlUp(0);

                            if (orientationSet && VzSet && VxSet && VySet) {
                                Broadcaster.Log(new STULog {
                                    Sender = MissileName,
                                    Message = "Entering flight phase",
                                    Type = STULogType.WARNING,
                                    Metadata = GetTelemetryDictionary()
                                });
                                phase = LaunchPhase.Flight;
                                ArmWarheads();

                            }
                            break;


                        case LaunchPhase.Flight:

                            FlightController.OrientShip(TestTarget);
                            FlightController.ControlForward(500);
                            FlightController.ControlRight(0);
                            FlightController.ControlUp(0);

                            if (Vector3D.Distance(FlightController.CurrentPosition, TestTarget) < 15) {
                                phase = LaunchPhase.Terminal;
                            }

                            break;

                        case LaunchPhase.Terminal:

                            SelfDestruct();
                            break;

                    }

                }


            }
        }
    }
}
