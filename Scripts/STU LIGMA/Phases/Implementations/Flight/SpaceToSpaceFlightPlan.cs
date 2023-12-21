﻿using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class SpaceToSpaceFlightPlan : IFlightPlan {

                Vector3D LaunchPos;
                Vector3D TargetPos;

                public SpaceToSpaceFlightPlan(Vector3D launchPos, Vector3D targetPos) {
                    LaunchPos = launchPos;
                    TargetPos = targetPos;
                }

                public override bool Run() {
                    var velocityStable = FlightController.SetStableForwardVelocity(50);
                    var orientationStable = FlightController.OrientShip(TargetPos);
                    if (velocityStable && orientationStable) {
                        Broadcaster.Log(new STULog {
                            Sender = MissileName,
                            Message = $"Runs for roll version",
                            Type = STULogType.WARNING,
                            Metadata = GetTelemetryDictionary()
                        });
                        return true;
                    }
                    return false;
                }

            }

        }
    }
}
