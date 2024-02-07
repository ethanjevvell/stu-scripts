using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {
            public class InterplanetaryFlightPlan : IFlightPlan {

                private enum FlightPhase {
                    Start,
                    StraightFlight,
                    LaunchPlanetOrbit,
                    TargetPlanetOrbit,
                }

                private const double FLIGHT_VELOCITY = 300;
                private const int TOTAL_ORBITAL_WAYPOINTS = 12;
                private const double FIRST_ORBIT_WAYPOINT_COEFFICIENT = 0.6;

                private FlightPhase CurrentPhase = FlightPhase.Start;

                private STUOrbitHelper LaunchPlanetOrbitHelper;
                private STUOrbitHelper TargetPlanetOrbitHelper;

                Vector3D ApproximateFlightStart;

                public InterplanetaryFlightPlan() {
                    // Find where LIGMA will be when this flight plan starts
                    Vector3D forwardVector = FlightController.CurrentWorldMatrix.Forward;
                    ApproximateFlightStart = FlightController.CurrentPosition + forwardVector * IntraplanetaryLaunchPlan.ELEVATION_CUTOFF;

                    if (NeedLaunchPlanetOrbit()) {
                        LaunchPlanetOrbitHelper = new STUOrbitHelper(TOTAL_ORBITAL_WAYPOINTS, FIRST_ORBIT_WAYPOINT_COEFFICIENT);
                        LaunchPlanetOrbitHelper.GeneratePlanetToSpaceOrbitalPath();
                        CreateOkBroadcast($"Created launch planet orbital plan for {LaunchPlanet.Value.Name}");

                        // If we need to orbit the target planet, we'll do that after we've orbited the launch planet,
                        // starting from the last point of the launch planet orbit
                        Vector3D finalLaunchOrbitPoint = LaunchPlanetOrbitHelper.OptimalOrbitalPath[LaunchPlanetOrbitHelper.OptimalOrbitalPath.Count - 1];

                        if (NeedTargetPlanetOrbit(finalLaunchOrbitPoint)) {
                            TargetPlanetOrbitHelper = new STUOrbitHelper(TOTAL_ORBITAL_WAYPOINTS, FIRST_ORBIT_WAYPOINT_COEFFICIENT);
                            TargetPlanetOrbitHelper.GenerateSpaceToPlanetOrbitalPath(finalLaunchOrbitPoint);
                            CreateOkBroadcast($"Created target planet orbital plan for {TargetPlanet.Value.Name}");
                            // No need to evaluate remaining scenarios
                            return;
                        }
                    }

                    if (NeedTargetPlanetOrbit(ApproximateFlightStart)) {
                        CurrentPhase = FlightPhase.TargetPlanetOrbit;
                        TargetPlanetOrbitHelper = new STUOrbitHelper(TOTAL_ORBITAL_WAYPOINTS, FIRST_ORBIT_WAYPOINT_COEFFICIENT);
                        TargetPlanetOrbitHelper.GenerateSpaceToPlanetOrbitalPath(ApproximateFlightStart);
                        CreateOkBroadcast($"Created target planet orbital plan for {TargetPlanet.Value.Name}");
                        return;
                    }

                    CreateOkBroadcast("No orbital plan needed");

                }

                public override bool Run() {

                    switch (CurrentPhase) {

                        case FlightPhase.Start:
                            if (LaunchPlanetOrbitHelper == null && TargetPlanetOrbitHelper == null) {
                                CurrentPhase = FlightPhase.StraightFlight;
                            } else if (LaunchPlanetOrbitHelper != null) {
                                CurrentPhase = FlightPhase.LaunchPlanetOrbit;
                            } else if (TargetPlanetOrbitHelper != null) {
                                CurrentPhase = FlightPhase.TargetPlanetOrbit;
                            }
                            break;

                        case FlightPhase.StraightFlight:
                            StraightFlight();
                            break;

                        case FlightPhase.LaunchPlanetOrbit:
                            var finishedLaunchPlanetOrbit = LaunchPlanetOrbit();
                            if (finishedLaunchPlanetOrbit) {
                                if (TargetPlanetOrbitHelper == null) {
                                    return true;
                                }
                                CurrentPhase = FlightPhase.TargetPlanetOrbit;
                            }
                            break;

                        case FlightPhase.TargetPlanetOrbit:
                            var finishedTargetPlanetOrbit = TargetPlanetOrbit();
                            if (finishedTargetPlanetOrbit) {
                                return true;
                            }
                            break;

                    }

                    return true;

                }

                private bool LaunchPlanetOrbit() {
                    return LaunchPlanetOrbitHelper.MaintainOrbitalFlight(FLIGHT_VELOCITY);
                }

                private bool TargetPlanetOrbit() {
                    return TargetPlanetOrbitHelper.MaintainOrbitalFlight(FLIGHT_VELOCITY);
                }

                private bool NeedLaunchPlanetOrbit() {
                    foreach (var kvp in LIGMA_VARIABLES.CelestialBodies) {
                        LIGMA_VARIABLES.Planet planet = kvp.Value;
                        BoundingSphere boundingSphere = new BoundingSphere(planet.Center, (float)planet.Radius);
                        bool lineIntersectsPlanet = STUOrbitHelper.LineIntersectsSphere(ApproximateFlightStart, TargetData.Position, boundingSphere);
                        // If the line intersects the launch planet, we need to orbit it
                        if (lineIntersectsPlanet && planet.Name == LaunchPlanet.Value.Name) {
                            return true;
                        }
                    }
                    return false;
                }

                private bool NeedTargetPlanetOrbit(Vector3D startPos) {
                    foreach (var kvp in LIGMA_VARIABLES.CelestialBodies) {
                        LIGMA_VARIABLES.Planet planet = kvp.Value;
                        BoundingSphere boundingSphere = new BoundingSphere(planet.Center, (float)planet.Radius);
                        bool lineIntersectsPlanet = STUOrbitHelper.LineIntersectsSphere(startPos, TargetData.Position, boundingSphere);
                        if (lineIntersectsPlanet && planet.Name == TargetPlanet.Value.Name) {
                            return true;
                        }
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

            }
        }
    }
}

