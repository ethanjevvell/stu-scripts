using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class OrbitalWaypoint {
                public Vector3D Position;
                public double Distance;
                public OrbitalWaypoint(Vector3D point, double distance) {
                    Position = point;
                    Distance = distance;
                }
            }

            public class STUOrbitHelper {

                // How many orbital waypoints will be constructed around the planet
                public int TotalOrbitalWaypoints = 12;
                // Will be mulitplied by the max orbit altitude to get the altitude of the first waypoint
                public double FirstOrbitWaypointCoefficient = 0.6;
                private int waypointIndex = 0;

                public STUOrbitHelper(int orbitalWaypoints, double firstWaypointCoefficient) {
                    TotalOrbitalWaypoints = orbitalWaypoints;
                    FirstOrbitWaypointCoefficient = firstWaypointCoefficient;
                    var allWaypoints = GenerateAllOrbitalWaypoints((Vector3D)LaunchPlanet?.Center, (double)LaunchPlanet?.Radius, LaunchCoordinates, TargetData.Position);
                    OptimalOrbitalPath = GetOptimalOrbitalPath(TargetData.Position, allWaypoints);
                }

                public List<Vector3D> OptimalOrbitalPath = new List<Vector3D>();

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
                    for (int i = 0; i < TotalOrbitalWaypoints; i++) {
                        double theta = 2 * Math.PI * i / TotalOrbitalWaypoints;
                        Vector3D point = center + orbitRadius * (Math.Cos(theta) * u + Math.Sin(theta) * v);
                        points.Add(point);
                    }

                    // The first point is scaled down to be closer to the planet
                    points[0] = center + (planetRadius + FirstOrbitWaypointCoefficient * maxOrbitAltitude) * (Math.Cos(0) * u + Math.Sin(0) * v);
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

                public bool MaintainOrbitalFlight(double desiredVelocity) {
                    while (waypointIndex < OptimalOrbitalPath.Count) {
                        var currentWaypoint = OptimalOrbitalPath[waypointIndex];
                        FlightController.SetStableForwardVelocity(desiredVelocity);
                        FlightController.AlignShipToTarget(currentWaypoint);
                        FlightController.OptimizeShipRoll(currentWaypoint);

                        if (Vector3D.Distance(OptimalOrbitalPath[waypointIndex], FlightController.CurrentPosition) < desiredVelocity) {
                            waypointIndex++;
                            CreateWarningBroadcast("Starting waypoint " + waypointIndex);
                        }

                        return false;
                    }

                    return true;
                }

                public static bool LineIntersectsSphere(Vector3D point1, Vector3D point2, BoundingSphere sphere) {
                    // Direction vector of the line
                    Vector3D lineDir = point2 - point1;
                    lineDir.Normalize();

                    // Vector from point1 to the sphere's center
                    Vector3D toSphereCenter = sphere.Center - point1;

                    // Project toSphereCenter onto lineDir to find the closest point on the line to the sphere's center
                    double t = Vector3D.Dot(toSphereCenter, lineDir);
                    Vector3D closestPoint = point1 + t * lineDir;

                    // Check if the closest point is within the line segment
                    if (t < 0 || t > Vector3D.Distance(point1, point2)) {
                        return false; // Closest point not within the segment
                    }

                    // Calculate the distance from the closest point to the sphere's center
                    double distanceToCenter = Vector3D.Distance(closestPoint, sphere.Center);

                    // Check if this distance is less than the sphere's radius
                    return distanceToCenter <= sphere.Radius;
                }


            }

        }
    }
}
