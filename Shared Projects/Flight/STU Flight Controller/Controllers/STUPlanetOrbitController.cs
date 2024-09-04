
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

              const double VELOCITY_ERROR_TOLERANCE = 0.5;

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

                    if (WithinVelocityErrorTolerance()) {
                      State = PointOrbitState.AdjustingVelocity;
                      break;
                    }

                    if (TargetAltitude - FlightController.AltitudeController.GetSeaLevelAltitude()) {
                      State = PointOrbitState.AdjustingAltitude;
                      break;
                    }

                    break;

                  case PointOrbitState.AdjustingVelocity:
                    if (AdjustVelocity()) {
                      State = PointOrbitState.SwitchOff;
                    }
                    break;

                  case PlanetOrbitState.SwitchOff:
                    // turn off thrusters
                    State = PlanetOrbitState.Idle;
                    break;

                  case PlanetOrbitState.SwitchOn:
                    // turn on thrusters
                    State = PlanetOrbitState.Idle;
                    break;

                }

                return false;

              }

              private bool AdjustVelocity() {
                    // One tick of velocity to get started
                    if (FlightController.CurrentVelocity == 0) {
                      Vector3D gravityVector = RemoteControl.GetNaturalGravity();
                      Vector3D initialOrbitVector = Vector3D.Cross(gravityVector, Planet.Center - FlightController.CurrentPosition);
                      Vector3D kickstartVelocityForce = Vector3D.Normalize(initialOrbitVector) * STUVelocityController.ShipMass;
                      FlightController.ExertVectorForce(Vector3D.TransformNormal(kickstartVelocityForce, MatrixD.Transpose(FlightController.CurrentWorldMatrix)) * new Vector3D(1, 1, -1));
                      return false;
                    }

                    Vector3D velocityVector = FlightController.RemoteControl.GetVelocity().LinearVelocities;
                    Vector3D velocityUnitVector = Vector3D.Normalize(velocityVector);

                    double velocityMagnitude = velocityVector.Length();
                    double velocityError = TargetVelocity - velocityMagnitude;

                    if (WithinVelocityErrorTolerance()) {
                      return true;
                    }

                    double outputForce = STUVelocityController.ShipMass * velocityError;
                    Vector3D outputForceVector = velocityUnitVector * outputForce;

                    Vector3D transformedOutputForceVector = Vector3D.TransformNormal(outputForceVector, MatrixD.Transpose(FlightController.CurrentWorldMatrix)) * new Vector3D(1, 1, -1)):

                    FlightController.ExertForceVector(transformedOutputForceVector);
                    return false;
              }

              private bool WithinVelocityErrorTolerance() {
                return Math.Abs(TargetVelocity - FlightController.CurrentVelocity) < VELOCITY_ERROR_TOLERANCE;
              }

            }
        }
    }
}
