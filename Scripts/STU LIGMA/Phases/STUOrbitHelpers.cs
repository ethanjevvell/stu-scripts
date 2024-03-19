using System;
using System.Collections.Generic;
using System.Linq;
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
                }

                public List<Vector3D> OptimalOrbitalPath = new List<Vector3D>();

                public void GenerateIntraplanetaryOrbitalPath() {
                    var allWaypoints = GenerateAllOrbitalWaypoints((Vector3D)LaunchPlanet?.Center, (double)LaunchPlanet?.Radius, LaunchCoordinates, TargetData.Position);
                    var orbitalPath = GetOptimalOrbitalPath(TargetData.Position, allWaypoints);
                    OptimalOrbitalPath = orbitalPath;
                }

                public void GeneratePlanetToSpaceOrbitalPath() {

                    var allWaypoints = GenerateAllOrbitalWaypoints((Vector3D)LaunchPlanet?.Center, (double)LaunchPlanet?.Radius, LaunchCoordinates, TargetData.Position);
                    var orbitalPath = GetOptimalOrbitalPath(TargetData.Position, allWaypoints);

                    Vector3D finalOrbitalPoint = orbitalPath[orbitalPath.Count - 1];
                    Vector3D targetPosition = TargetData.Position;

                    Vector3D centerToFinalOrbitalPoint = finalOrbitalPoint - (Vector3D)LaunchPlanet?.Center;
                    Vector3D centerToTargetPoint = targetPosition - (Vector3D)LaunchPlanet?.Center;

                    double centerToFinalOrbitalPointDistance = Vector3D.Distance(finalOrbitalPoint, (Vector3D)LaunchPlanet?.Center);
                    double centerToTargetPointDistance = Vector3D.Distance(targetPosition, (Vector3D)LaunchPlanet?.Center);

                    if (centerToTargetPointDistance > centerToFinalOrbitalPointDistance) {
                        CreateWarningBroadcast("Finding early breakoff orbit");
                        orbitalPath = findEarlyBreakoffOrbit(orbitalPath);
                        CreateOkBroadcast(orbitalPath[orbitalPath.Count - 1].ToString());
                    }

                    OptimalOrbitalPath = orbitalPath;

                }

                public void GenerateSpaceToPlanetOrbitalPath(Vector3D startPos) {

                    var allWaypoints = GenerateAllOrbitalWaypoints((Vector3D)TargetPlanet?.Center, (double)TargetPlanet?.Radius, startPos, TargetData.Position);
                    var orbitalPath = GetOptimalOrbitalPath(TargetData.Position, allWaypoints);

                    Vector3D finalOrbitalPoint = orbitalPath[orbitalPath.Count - 1];
                    Vector3D centerToFinalOrbitalPoint = finalOrbitalPoint - (Vector3D)TargetPlanet?.Center;

                    double centerToFinalOrbitalPointDistance = Vector3D.Distance(finalOrbitalPoint, (Vector3D)TargetPlanet?.Center);
                    double centerToLaunchPointDistance = Vector3D.Distance(startPos, (Vector3D)TargetPlanet?.Center);

                    if (centerToLaunchPointDistance > centerToFinalOrbitalPointDistance) {
                        CreateWarningBroadcast("Finding early entry orbit");
                        orbitalPath = findEarlyEntryOrbit(orbitalPath);
                    }

                    OptimalOrbitalPath = orbitalPath;

                }

                private List<Vector3D> findEarlyBreakoffOrbit(List<Vector3D> orbit) {

                    if (orbit.Count <= 2) {
                        return orbit;
                    }

                    double[] angles = new double[orbit.Count];

                    for (int i = 0; i < orbit.Count - 1; i++) {
                        angles[i] = findAngleBetween(orbit[i], orbit[i + 1], TargetData.Position);
                    }

                    // find the index of the angle closest to pi
                    int optimalExitPointIndex = 0;
                    double min = Math.Abs(angles[0] - Math.PI);
                    for (int i = 1; i < angles.Length; i++) {
                        double currentAngleDifference = Math.Abs(angles[i] - Math.PI);
                        if (currentAngleDifference < min) {
                            min = currentAngleDifference;
                            // we want to exit at the point AFTER that with the lowest angle value
                            optimalExitPointIndex = i + 1;
                        }
                    }

                    // Edge case: The optimal exit point is the last point in the orbit
                    if (optimalExitPointIndex == orbit.Count - 1) {
                        return orbit;
                    }

                    if (optimalExitPointIndex >= orbit.Count) {
                        CreateFatalErrorBroadcast("Optimal exit point index is out of range; aborting mission");
                    }

                    // .Take() slices from the beginning of the list to the specified index, but is exclusive of that index
                    orbit = orbit.Take(optimalExitPointIndex + 1).ToList();
                    CreateOkBroadcast($"Early exit: {orbit[orbit.Count - 1]}");
                    return orbit;

                }

                private List<Vector3D> findEarlyEntryOrbit(List<Vector3D> orbit) {
                    if (orbit.Count <= 2) {
                        return orbit;
                    }

                    double[] angles = new double[orbit.Count];
                    for (int i = 0; i < orbit.Count - 1; i++) {
                        angles[i] = findAngleBetween(LaunchCoordinates, orbit[i], orbit[i + 1]);
                    }

                    CreateOkBroadcast("Angles: " + string.Join(", ", angles));

                    // find the index of the angle closest to pi
                    int optimalEntryPointIndex = 0;
                    double min = Math.Abs(angles[0] - Math.PI);
                    for (int i = 1; i < angles.Length; i++) {
                        double currentAngleDifference = Math.Abs(angles[i] - Math.PI);
                        if (currentAngleDifference < min) {
                            min = currentAngleDifference;
                            // draw out the geometry and it becomes clear why we enter at index i instead of i + 1, which we did for early breakoff
                            optimalEntryPointIndex = i;
                        }
                    }

                    if (optimalEntryPointIndex >= orbit.Count) {
                        CreateFatalErrorBroadcast("Optimal entry point index is out of range; aborting mission");
                    }

                    CreateOkBroadcast("Finished early orbit creation routine");
                    // slice from optimal entry point to the end of the list
                    orbit = orbit.GetRange(optimalEntryPointIndex, orbit.Count - optimalEntryPointIndex);
                    return orbit;
                }

                private double findAngleBetween(Vector3D currentPoint, Vector3D nextPoint, Vector3D targetPoint) {
                    Vector3D NC = currentPoint - nextPoint;
                    Vector3D NT = targetPoint - nextPoint;
                    return Math.Acos(Vector3D.Dot(NC, NT) / (NC.Length() * NT.Length()));
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

                    double maxOrbitAltitude = planetRadius * 0.77f;
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
