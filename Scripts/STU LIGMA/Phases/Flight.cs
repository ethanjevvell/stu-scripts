using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class FlightPhase {

                // How many orbital waypoints will be constructed around the planet
                private const int numWaypoints = 12;
                // How far the orbital waypoints will be from the planet's center
                // MINIMUM of 1.4; if you set to 1.0, the "orbit" will be a circle directly on the
                // surface of the planet, which is not what we want
                private const double radiusCoefficient = 1.4;

                private int nextWaypoint = 0;

                List<Vector3D> FlightWaypoints;

                public struct Planet {
                    public string Name;
                    public double Radius;
                    public Vector3D Center;
                }

                public class OrbitalWaypoint {

                    public Vector3D Position;
                    public double Distance;

                    public OrbitalWaypoint(Vector3D point, double distance) {
                        Position = point;
                        Distance = distance;
                    }
                }

                public Planet TestEarth = new Planet {
                    Name = "TestEarth",
                    Radius = 61050.39,
                    // Center of the planet in the solar system world, which is convenient
                    Center = new Vector3D(0, 0, 0)
                };

                public FlightPhase(Vector3D launch, Vector3D target, Action<string> echo) {
                    var orbitalWaypoints = GenerateAllOrbitalWaypoints(TestEarth.Center, TestEarth.Radius, launch, target);
                    FlightWaypoints = GetOptimalOrbitalPath(target, orbitalWaypoints);
                    foreach (var point in FlightWaypoints) {
                        echo(point.ToString());
                    }
                }

                public bool Run() {

                    while (nextWaypoint < FlightWaypoints.Count) {
                        FlightController.SetStableForwardVelocity(500);
                        FlightController.OrientShip(FlightWaypoints[nextWaypoint]);

                        if (Vector3D.Distance(FlightWaypoints[nextWaypoint], FlightController.CurrentPosition) < 1000) {
                            nextWaypoint++;
                            Broadcaster.Log(new STULog {
                                Sender = MissileName,
                                Message = "Starting waypoint " + nextWaypoint,
                                Type = STULogType.WARNING,
                                Metadata = GetTelemetryDictionary()
                            });
                        }

                        return false;
                    }

                    return true;

                }

                private List<Vector3D> GenerateAllOrbitalWaypoints(Vector3D center, double radius, Vector3D pointA, Vector3D pointB) {
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

                    // Circle's radius
                    double circleRadius = radiusCoefficient * radius;

                    // Generate points on the circle
                    var points = new List<Vector3D>();
                    for (int i = 0; i < numWaypoints; i++) {
                        double theta = 2 * Math.PI * i / numWaypoints;
                        Vector3D point = center + circleRadius * (Math.Cos(theta) * u + Math.Sin(theta) * v);
                        points.Add(new Vector3D(Math.Round(point.X, 2), Math.Round(point.Y, 2), Math.Round(point.Z, 2)));
                    }

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
