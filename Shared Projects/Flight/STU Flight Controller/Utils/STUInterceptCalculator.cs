using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class STUFlightController {

            /// <summary>
            /// This class will determine at what time and position a given point will intercept another
            /// point on a 2d plane.
            /// </summary>
            public class STUInterceptCalculator {
                /// <summary>
                /// INPUT: The Position Vector for the chasing object.
                /// </summary>
                private Vector3D chaserPosition = Vector3D.Zero;

                /// <summary>
                /// INPUT: The speed of the chasing object
                /// </summary>
                private double chaserSpeed = double.NaN;

                /// <summary>
                /// INPUT: The position of the object being chased (the Runner)
                /// </summary>
                private Vector3D runnerPosition = Vector3D.Zero;

                /// <summary>
                /// INPUT: The Velocity of the Runner
                /// </summary>
                private Vector3D runnerVelocity = Vector3D.Zero;

                /// <summary>
                /// OUTPUT: TRUE if the interception is possible given the inputs
                /// </summary>
                private bool interceptionPossible = false;

                /// <summary>
                /// OUTPUT: If "IsAVector" and InterceptionPossible, this is the velocity that the
                /// Chaser should have in order to intercept the Runner.
                /// </summary>
                private Vector3D chaserVelocity = Vector3D.Zero;

                /// <summary>
                /// OUTPUT: If "IsAVector" and InterceptionPossible, this is the point at which
                /// interception shall occur, given that the Chaser immmediately assumes ChaserVelocity
                /// </summary>
                private Vector3D interceptionPosition = Vector3D.Zero;

                /// <summary>
                /// OUTPUT: If not Nan and InterceptionPossible, this is the The amount of time that
                /// must pass before interception.
                /// </summary>
                private double timeToInterception = double.NaN;

                /// <summary>
                /// Set to TRUE when the routine is finished.
                /// </summary>
                private bool calculationPerformed = false;

                /// <summary>
                /// INPUT: The Position Vector for the chasing object.
                /// </summary>
                public Vector3D ChaserPosition {
                    get { return chaserPosition; }
                    set {
                        ClearResults();
                        chaserPosition = value;
                    }
                }

                /// <summary>
                /// INPUT: The speed of the chasing object
                /// </summary>
                public double ChaserSpeed {
                    get { return chaserSpeed; }
                    set {
                        ClearResults();
                        chaserSpeed = value;
                    }
                }

                /// <summary>
                /// INPUT: The position of the object being chased (the Runner)
                /// </summary>
                public Vector3D RunnerPosition {
                    get { return runnerPosition; }
                    set {
                        ClearResults();
                        runnerPosition = value;
                    }
                }

                /// <summary>
                /// INPUT: The Velocity of the Runner
                /// </summary>
                public Vector3D RunnerVelocity {
                    get { return runnerVelocity; }
                    set {
                        ClearResults();
                        runnerVelocity = value;
                    }
                }

                /// <summary>
                /// OUTPUT: If "IsAVector" and InterceptionPossible, this is the point at which
                /// interception shall occur, given that the Chaser immmediately assumes ChaserVelocity
                /// </summary>
                public Vector3D InterceptionPoint {
                    get {
                        SetResults();
                        return interceptionPosition;
                    }
                }

                /// <summary>
                /// OUTPUT: If "IsAVector" and InterceptionPossible, this is the velocity that the
                /// Chaser should have in order to intercept the Runner.
                /// </summary>
                public Vector3D ChaserVelocity {
                    get {
                        SetResults();
                        return chaserVelocity;
                    }
                }

                /// <summary>
                /// OUTPUT: If not Nan and InterceptionPossible, this is the The amount of time that
                /// must pass before interception.
                /// </summary>
                public double TimeToInterception {
                    get {
                        SetResults();
                        return timeToInterception;
                    }
                }

                /// <summary>
                /// OUTPUT: TRUE if the interception is possible given the inputs
                /// </summary>
                public bool InterceptionPossible {
                    get {
                        SetResults();
                        return interceptionPossible;
                    }
                }

                /// <summary>
                /// Call to force a re-calculation. Re-calculation will also happen any time one of the
                /// INPUT properties changes.
                /// </summary>
                public void ClearResults() {
                    calculationPerformed = false;
                    interceptionPossible = false;
                    chaserVelocity = Vector3D.Zero;
                    interceptionPosition = Vector3D.Zero;
                    timeToInterception = double.NaN;
                }

                /// <summary>
                /// Determine if all of the input values are valid. By default, they are all invalid,
                /// forcing the user of this class to set them all before a calculation will be
                /// performed.
                /// </summary>
                public bool HasValidInputs {
                    get {
                        return
                                !chaserPosition.IsZero() &&
                                !runnerPosition.IsZero() &&
                                !runnerVelocity.IsZero() &&
                               !double.IsNaN(chaserSpeed) &&
                               !double.IsInfinity(chaserSpeed);
                    }
                }

                /// <summary>
                /// Called internally to calculate the interception data.
                /// </summary>
                private void SetResults() {
                    // Don't re-calculate if none of the input parameters have changed.
                    if (calculationPerformed) {
                        return;
                    }

                    // Make sure all results look like "no interception possible".
                    ClearResults();

                    // Set this to TRUE regardless of the success or failure of interception. This
                    // prevents this routine from doing anything until one of the input values has been
                    // changed or the application calls ClearResults()
                    calculationPerformed = true;

                    // If the inputs are invalid, then everything is already set for a "no interception"
                    // scenario.
                    if (!HasValidInputs) {
                        return;
                    }

                    // First check- Are we already on top of the target? If so, its valid and we're done
                    if (ChaserPosition == RunnerPosition) {
                        interceptionPossible = true;
                        interceptionPosition = ChaserPosition;
                        timeToInterception = 0;
                        chaserVelocity = Vector3D.Zero;
                        return;
                    }

                    // Check- Am I moving? Be gracious about exception throwing even though negative
                    // speed is undefined.
                    if (ChaserSpeed <= 0) {
                        return; // No interception
                    }

                    Vector3D vectorFromRunner = ChaserPosition - RunnerPosition;
                    double distanceToRunner = vectorFromRunner.Length();
                    double runnerSpeed = RunnerVelocity.Length();

                    // Check- Is the Runner not moving? If it isn't, the calcs don't work because we
                    // can't use the Law of Cosines
                    if (Math.Abs(runnerSpeed) < 1e-3) {
                        timeToInterception = distanceToRunner / ChaserSpeed;
                        interceptionPosition = RunnerPosition;
                    } else {
                        // We can do the Law of Cosines approach
                        double a = ChaserSpeed * ChaserSpeed - runnerSpeed * runnerSpeed;
                        double b = 2 * vectorFromRunner.Dot(RunnerVelocity);
                        double c = -distanceToRunner * distanceToRunner;

                        double t1, t2;
                        if (!QuadraticSolver(a, b, c, out t1, out t2)) {
                            // No real-valued solution, so no interception possible
                            return;
                        }

                        if (t1 < 0 && t2 < 0) {
                            // Both values for t are negative, so the interception would have to have
                            // occured in the past
                            return;
                        }

                        if (t1 > 0 && t2 > 0) // Both are positive, take the smaller one
                            timeToInterception = Math.Min(t1, t2);
                        else // One has to be negative, so take the larger one
                            timeToInterception = Math.Max(t1, t2);

                        interceptionPosition = RunnerPosition + RunnerVelocity * timeToInterception;
                    }

                    // Calculate the resulting velocity based on the time and intercept position
                    chaserVelocity = (interceptionPosition - ChaserPosition) / timeToInterception;

                    // Finally, signal that the interception was possible.
                    interceptionPossible = true;
                }
            }

            /// <summary>
            /// Solve a quadratic equation in the form ax^2 + bx + c = 0
            /// </summary>
            /// <param name="a">Coefficient for x^2</param>
            /// <param name="b">Coefficient for x</param>
            /// <param name="c">Constant</param>
            /// <param name="solution1">The first solution</param>
            /// <param name="solution2">The second solution</param>
            /// <returns>TRUE if a solution exists, FALSE if one does not</returns>
            public static bool QuadraticSolver(double a, double b, double c, out double solution1, out double solution2) {
                if (a == 0) {
                    if (b == 0) {
                        solution1 = solution2 = double.NaN;
                        return false;
                    } else {
                        solution1 = solution2 = -c / b;
                        return true;
                    }
                }

                double det = b * b - 4 * a * c;
                if (det < 0) {
                    solution1 = solution2 = double.NaN;
                    return false;
                }

                det = Math.Sqrt(det);
                double _2a = 2 * a;
                solution1 = (-b + det) / _2a;
                solution2 = (-b - det) / _2a;
                return true;
            }

        }
    }
}

