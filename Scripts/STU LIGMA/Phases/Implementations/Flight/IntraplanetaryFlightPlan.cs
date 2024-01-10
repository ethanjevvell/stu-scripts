using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class IntraplanetaryFlightPlan : IFlightPlan {

                // How many orbital waypoints will be constructed around the planet
                private const int TOTAL_ORBITAL_WAYPOINTS = 12;
                // Will be mulitplied by the max orbit altitude to get the altitude of the first waypoint
                private const double FIRST_ORBIT_WAYPOINT_COEFFICIENT = 0.6;
                private const int FLIGHT_VELOCITY = 200;
                private int waypointIndex = 0;

                List<Vector3D> FlightWaypoints;

                public class OrbitalWaypoint {
                    public Vector3D Position;
                    public double Distance;
                    public OrbitalWaypoint(Vector3D point, double distance) {
                        Position = point;
                        Distance = distance;
                    }
                }

                public IntraplanetaryFlightPlan() {
                    var orbitalWaypoints = GenerateAllOrbitalWaypoints((Vector3D)LaunchPlanet?.Center, (double)LaunchPlanet?.Radius, LaunchCoordinates, TargetData.Position);
                    FlightWaypoints = GetOptimalOrbitalPath(TargetData.Position, orbitalWaypoints);
                }

                public override bool Run() {

                    while (waypointIndex < FlightWaypoints.Count) {

                        var currentWaypoint = FlightWaypoints[waypointIndex];
                        FlightController.SetStableForwardVelocity(FLIGHT_VELOCITY);
                        FlightController.AlignShipToTarget(currentWaypoint);
                        FlightController.OptimizeShipRoll(currentWaypoint);

                        if (Vector3D.Distance(FlightWaypoints[waypointIndex], FlightController.CurrentPosition) < FLIGHT_VELOCITY) {
                            waypointIndex++;
                            CreateWarningBroadcast("Starting waypoint " + waypointIndex);
                        }

                        return false;
                    }

                    return true;

                }

                private List<Vector3D> GenerateAllOrbitalWaypoints(Vector3D center, double planetRadius, Vector3D pointA, Vector3D pointB) {
                    // Calculate vectors CA and CB
                    Vector3D CA = pointA - center;
                    Vector3D CB = pointB - center;

                    // Normal vector of the plane (cross product of CA and CB)
                    Vector3D normal = Vector3D.Cross(CA, CB);

                    // Find one basis vector on the plane (Normalized CA)
                    // It will point directly at pointA
                    Vector3D u = Vector3D.Normalize(CA);

                    // Find another basis vector on the plane (cross product of normal and U)
                    Vector3D v = Vector3D.Cross(normal, u);
                    v = Vector3D.Normalize(v);

                    double maxOrbitAltitude = planetRadius;
                    double orbitRadius = planetRadius + maxOrbitAltitude;

                    // Generate points on the circle
                    var points = new List<Vector3D>();
                    for (int i = 0; i < TOTAL_ORBITAL_WAYPOINTS; i++) {
                        double theta = 2 * Math.PI * i / TOTAL_ORBITAL_WAYPOINTS;
                        Vector3D point = center + orbitRadius * (Math.Cos(theta) * u + Math.Sin(theta) * v);
                        points.Add(point);
                    }

                    // The first point is scaled down to be closer to the planet
                    points[0] = center + (planetRadius + FIRST_ORBIT_WAYPOINT_COEFFICIENT * maxOrbitAltitude) * (Math.Cos(0) * u + Math.Sin(0) * v);
                    return points;
                }

                private List<Vector3D> GetOptimalOrbitalPath(Vector3D targetPoint, List<Vector3D> orbitalWaypoints) {
                    // Get each point's distance from the target point and store as an OrbitalWaypoint
                    var orbitalPointDistancesFromTarget = new List<OrbitalWaypoint>();
                    foreach (var point in orbitalWaypoints) {
                        double distance = Vector3D.Distance(targetPoint, point);
                        orbitalPointDistancesFromTarget.Add(new OrbitalWaypoint(point, distance));
                    }

                    // Sort keys from shortest distance to the target point to the furthest distance
                    orbitalPointDistancesFromTarget.Sort((a, b) => a.Distance.CompareTo(b.Distance));

                    // the points themselves
                    var closestPointToTarget = orbitalPointDistancesFromTarget[0].Position;
                    var secondClosestPointToTarget = orbitalPointDistancesFromTarget[1].Position;
                    var thirdClosestPointToTarget = orbitalPointDistancesFromTarget[2].Position;

                    // the points' distances from the target
                    var secondClosestPointDistance = orbitalPointDistancesFromTarget[1].Distance;
                    var thirdClosestPointDistance = orbitalPointDistancesFromTarget[2].Distance;

                    var pathA = new List<Vector3D>(orbitalWaypoints);
                    // Create another list of points where the first point is the same but all others are reversed
                    var pathB = new List<Vector3D>(orbitalWaypoints);
                    pathB.RemoveAt(0); // Remove the first element before reversing
                    pathB.Reverse();   // Reverse the rest of the list
                    pathB.Insert(0, orbitalWaypoints[0]); // Re-insert the first element at the beginning

                    // Edge case: target point is almost directly below an orbital point, the second and third closest points are equidistant from the target point
                    if (Math.Abs(secondClosestPointDistance - thirdClosestPointDistance) < 1e-6) {
                        return FindShortestPath(pathA, pathB, secondClosestPointToTarget, thirdClosestPointToTarget);
                    }

                    return FindShortestPath(pathA, pathB, closestPointToTarget, secondClosestPointToTarget);
                }

                private static List<Vector3D> FindShortestPath(List<Vector3D> pathA, List<Vector3D> pathB, Vector3D targetOne, Vector3D targetTwo) {
                    for (int ind = 0; ind < pathA.Count; ind++) {
                        if (PointIsEqualToEither(pathA[ind], targetOne, targetTwo)) {
                            return pathA.GetRange(0, ind + 1);
                        }
                        if (PointIsEqualToEither(pathB[ind], targetOne, targetTwo)) {
                            return pathB.GetRange(0, ind + 1);
                        }
                    }

                    return new List<Vector3D>();
                }

                private static bool PointIsEqualToEither(Vector3D point, Vector3D targetOne, Vector3D targetTwo) {
                    return point == targetOne || point == targetTwo;
                }

            }

        }
    }
}
