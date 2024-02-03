using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class IntraplanetaryFlightPlan : IFlightPlan {

                private const double FLIGHT_VELOCITY = 140;
                private const int TOTAL_ORBITAL_WAYPOINTS = 12;
                private const double FIRST_ORBIT_WAYPOINT_COEFFICIENT = 0.6;

                private enum LaunchPhase {
                    Start,
                    StraightFlight,
                    CircumnavigatePlanet,
                    End
                }

                private LaunchPhase CurrentPhase = LaunchPhase.Start;
                private LIGMA_VARIABLES.Planet? PlanetToOrbit = null;
                private STUOrbitHelper OrbitHelper;

                public IntraplanetaryFlightPlan() {
                    // Find where LIGMA will be when this flight plan starts
                    Vector3D forwardVector = FlightController.CurrentWorldMatrix.Forward;
                    Vector3D approximateFlightStart = FlightController.CurrentPosition + forwardVector * IntraplanetaryLaunchPlan.ELEVATION_CUTOFF;

                    foreach (var kvp in LIGMA_VARIABLES.CelestialBodies) {
                        LIGMA_VARIABLES.Planet planet = kvp.Value;
                        BoundingSphere boundingSphere = new BoundingSphere(planet.Center, (float)planet.Radius);
                        bool lineIntersectsPlanet = STUOrbitHelper.LineIntersectsSphere(approximateFlightStart, TargetData.Position, boundingSphere);
                        if (lineIntersectsPlanet) {
                            PlanetToOrbit = planet;
                            OrbitHelper = new STUOrbitHelper(TOTAL_ORBITAL_WAYPOINTS, FIRST_ORBIT_WAYPOINT_COEFFICIENT);
                            OrbitHelper.GenerateStandardOrbitalPath();
                            CreateOkBroadcast($"Created orbital plan for {kvp.Key}");
                            return;
                        }
                    }

                    CreateOkBroadcast("No orbital plan needed");

                }

                public override bool Run() {

                    switch (CurrentPhase) {

                        case LaunchPhase.Start:
                            if (PlanetToOrbit == null) {
                                CurrentPhase = LaunchPhase.StraightFlight;
                            } else {
                                CurrentPhase = LaunchPhase.CircumnavigatePlanet;
                            }
                            break;

                        case LaunchPhase.StraightFlight:
                            var finishedStraightFlight = StraightFlight();
                            if (finishedStraightFlight) {
                                LIGMA.CreateOkBroadcast("Finished straight flight");
                                CurrentPhase = LaunchPhase.End;
                            }
                            break;

                        case LaunchPhase.CircumnavigatePlanet:
                            var finishedCircumnavigation = CircumnavigatePlanet();
                            if (finishedCircumnavigation) {
                                CurrentPhase = LaunchPhase.End;
                            }
                            break;

                        case LaunchPhase.End:
                            return true;

                    }

                    return false;

                }

                private bool StraightFlight() {
                    FlightController.OptimizeShipRoll(TargetData.Position);
                    FlightController.SetStableForwardVelocity(FLIGHT_VELOCITY);
                    var shipAligned = FlightController.AlignShipToTarget(TargetData.Position);

                    if (shipAligned) {
                        return true;
                    }

                    return false;
                }

                private bool CircumnavigatePlanet() {
                    return OrbitHelper.MaintainOrbitalFlight(FLIGHT_VELOCITY);
                }

            }
        }
    }
}
