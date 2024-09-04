
namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {
            public partial class STUPointOrbitController {

              enum PointOrbitState {
                Initialize,
                Idle,
                AdjustingVelocity,
                AdjustingAltitude,
                SwitchOff,
                SwitchOn
              }

              double TargetVelocity;
              double TargetAltitude;
              Planet? TargetPlanet; 

              PointOrbitState State;

              public STUPointOrbitController() {
                State = PointOrbitState.Initialize;
              }

              public bool Run() {

                switch (State) {

                  case PointOrbitState.Initialize:
                    // figure out TargetPlanet, using galactic map
                    double gravityMagnitudeAtOrbitAltitude = FlightController.VelocityController.LocalGravityVector.Length();
                    double targetRadius = Vector3D.Distance(RemoteControl.CenterOfMass, TargetPlanet.Center);
                    TargetVelocity = Math.sqrt(gravityMagnitudeAtOrbitAltitude * targetRadius);
                    TargetAltitude = FlightController.AltitudeController.GetSeaLevelAltitude();
                    State = PointOrbitState.Idle;

                  case PointOrbitState.Idle:

                    if (FlightController.CurrentVelocity < TargetVelocity) {
                      State = PointOrbitState.AdjustingVelocity;
                      break;
                    }

                    if (TargetAltitude - FlightController.AltitudeController.GetSeaLevelAltitude()) {
                      State = PointOrbitState.AdjustingAltitude;
                      break;
                    }

                    break;

                  case PointOrbitState.AdjustingVelocity:
                    if (FlightController.CurrentVelocity == 0) {
                      Vector3D gravityVector = FlightController.VelocityController.LocalGravityVector;
                      Vector3D initialOrbitVector = Vector3D.Cross(gravityVector, Planet.Center - FlightController.CurrentPosition);
                    }
                    break;

                }

                return false;

              }

              private bool AdjustVelocity() {

              }

            }
        }
    }
}
