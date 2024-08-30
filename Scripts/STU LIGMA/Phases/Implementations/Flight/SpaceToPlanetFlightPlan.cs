
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class SpaceToPlanetFlightPlan : IFlightPlan {

                private double FLIGHT_VELOCITY = 500;
                private double ELEVATION_CUTOFF = 7500;
                private double CurrentElevation;

                private const int TOTAL_ORBITAL_WAYPOINTS = 12;
                private const double FIRST_ORBIT_WAYPOINT_COEFFICIENT = 0.6;

                private enum FlightPhase {
                    Start,
                    StraightFlight,
                    CircumnavigatePlanet,
                    End
                }

                private FlightPhase CurrentPhase = FlightPhase.Start;
                private STUGalacticMap.Planet? PlanetToOrbit = null;
                private STUOrbitHelper OrbitHelper;

                public SpaceToPlanetFlightPlan() {

                    // Find where LIGMA will be when this flight plan starts
                    Vector3D forwardVector = FlightController.CurrentWorldMatrix.Forward;
                    CreateOkBroadcast($"Forward vector: {forwardVector}");
                    Vector3D approximateFlightStart = FlightController.CurrentPosition + forwardVector * SpaceToPlanetLaunchPlan.LAUNCH_DISTANCE;
                    CreateOkBroadcast($"Approximate flight start: {approximateFlightStart}");

                    foreach (var kvp in STUGalacticMap.CelestialBodies) {
                        STUGalacticMap.Planet planet = kvp.Value;
                        BoundingSphere boundingSphere = new BoundingSphere(planet.Center, (float)planet.Radius);
                        bool lineIntersectsPlanet = STUOrbitHelper.LineIntersectsSphere(approximateFlightStart, TargetData.Position, boundingSphere);
                        if (lineIntersectsPlanet) {
                            CreateOkBroadcast($"Line intersects {kvp.Key}");
                            PlanetToOrbit = planet;
                            OrbitHelper = new STUOrbitHelper(TOTAL_ORBITAL_WAYPOINTS, FIRST_ORBIT_WAYPOINT_COEFFICIENT);
                            CreateOkBroadcast($"Created orbit helper for {kvp.Key}");
                            OrbitHelper.GenerateSpaceToPlanetOrbitalPath(LaunchCoordinates);
                            CreateOkBroadcast($"Created orbital plan for {kvp.Key}");
                            return;
                        }
                    }

                    CreateOkBroadcast("No orbital plan needed");

                }

                public override bool Run() {

                    FirstRunTasks();

                    switch (CurrentPhase) {

                        case FlightPhase.Start:
                            if (PlanetToOrbit == null) {
                                CurrentPhase = FlightPhase.StraightFlight;
                            } else {
                                CurrentPhase = FlightPhase.CircumnavigatePlanet;
                            }
                            break;

                        case FlightPhase.StraightFlight:
                            StraightFlight();
                            if (RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out CurrentElevation)) {
                                if (CurrentElevation <= ELEVATION_CUTOFF) {
                                    return true;
                                }
                            }
                            break;

                        case FlightPhase.CircumnavigatePlanet:
                            var finishedCircumnavigation = CircumnavigatePlanet();
                            if (finishedCircumnavigation) {
                                CurrentPhase = FlightPhase.End;
                            }
                            break;

                        case FlightPhase.End:
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

