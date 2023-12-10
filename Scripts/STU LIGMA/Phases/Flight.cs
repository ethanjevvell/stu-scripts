using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public static class Flight {

                // space world "Test Target"
                //private static Vector3D TargetPositionOne = new Vector3D(484.92, 3816.29, -1834.40);

                private static Vector3D TargetPositionOne = new Vector3D(-60467.15, -6726.73, 4133.09);

                enum FlightPhase {
                    Start,
                    Flight,
                    End
                }

                static FlightPhase phase = FlightPhase.Start;
                private static bool velocityStable = false;
                private static bool orientationStable = false;

                public static bool Run() {

                    switch (phase) {

                        case FlightPhase.Start:

                            phase = FlightPhase.Flight;
                            Broadcaster.Log(new STULog {
                                Sender = MissileName,
                                Message = "Starting flight plan",
                                Type = STULogType.WARNING,
                                Metadata = GetTelemetryDictionary()
                            });

                            break;

                        case FlightPhase.Flight:

                            velocityStable = FlightController.SetStableForwardVelocity(10);
                            orientationStable = FlightController.OrientShip(TargetPositionOne);

                            if (velocityStable && orientationStable) {
                                phase = FlightPhase.End;
                                Broadcaster.Log(new STULog {
                                    Sender = MissileName,
                                    Message = "Entering End phase",
                                    Type = STULogType.OK,
                                    Metadata = GetTelemetryDictionary()
                                });
                            }

                            break;


                        case FlightPhase.End:

                            FlightController.SetStableForwardVelocity(100);
                            FlightController.OrientShip(TargetPositionOne);

                            if (Vector3D.Distance(FlightController.CurrentPosition, TargetPositionOne) < 20) {
                                SelfDestruct();
                                return true;
                            }

                            break;

                    }

                    return false;

                }

            }

        }
    }
}
