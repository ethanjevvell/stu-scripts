//using System.Collections.Generic;
//using VRageMath;

using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class LIGMA {

            public class TestSuite : ILaunchPlan {

                //                public enum LaunchPhase {
                //                    Idle,
                //                    PerformingManeuver,
                //                    Terminal
                //                }

                //                public enum Direction {
                //                    Forward,
                //                    Right,
                //                    Up
                //                }

                //                public struct ControlAction {
                //                    public Direction Direction;
                //                    public float Magnitude;
                //                }

                //                public struct Maneuver {
                //                    public List<ControlAction> Actions;
                //                }

                //                public static LaunchPhase phase = LaunchPhase.Idle;
                //                public static int currentManeuverIndex = 0;

                //                private static Maneuver Stop = new Maneuver {
                //                    Actions = new List<ControlAction> {
                //                        new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                        new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                //                        new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                    }
                //                };

                //                public static List<Maneuver> testSequence = new List<Maneuver> {

                //                    // Single-axis tests
                //                    #region
                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 5 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = -5 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = -50 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    Stop,


                //                    // another set of single-axis tests for right
                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 5 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Right, Magnitude = -5 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Right, Magnitude = -50 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    Stop,

                //                    // now for up

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 5 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Up, Magnitude = -5 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Up, Magnitude = -50 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                //                        }
                //                    },

                //                    Stop,
                //                    #endregion

                //                    // Two-axis tests
                //                    #region
                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 1 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 5 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = -1 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = -5 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = -50 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = -50 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                //                        }
                //                    },

                //                    Stop,

                //                    // now for forward and up

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 1 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 5 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = -1 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = -5 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = -50 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = -50 },
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                //                        }
                //                    },

                //                    Stop,

                //                    // now for right and up

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 1 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 5 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Right, Magnitude = -1 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = -5 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Right, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = 50 },
                //                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 }
                //                        }
                //                    },

                //                    new Maneuver {
                //                        Actions = new List<ControlAction> {
                //                             new ControlAction { Direction = Direction.Right, Magnitude = -50 },
                //                             new ControlAction { Direction = Direction.Up, Magnitude = -50 },
                //                             new ControlAction {Direction = Direction.Forward, Magnitude = 0},
                //                        }
                //                    },

                //                    Stop,
                //                    #endregion

                //                    // octant tests
                //                    #region
                //                    new Maneuver
                //                    {
                //                        Actions = new List<ControlAction>
                //                        {
                //                            new ControlAction {Direction = Direction.Forward, Magnitude = 10 },
                //                            new ControlAction {Direction = Direction.Up, Magnitude = 10 },
                //                            new ControlAction {Direction = Direction.Right, Magnitude = 10 },
                //                        },
                //                    },

                //                    Stop,

                //                    new Maneuver
                //                    {
                //                        Actions = new List<ControlAction>
                //                        {
                //                            new ControlAction {Direction = Direction.Forward, Magnitude = -10 },
                //                            new ControlAction {Direction = Direction.Up, Magnitude = 10 },
                //                            new ControlAction {Direction = Direction.Right, Magnitude = 10 },
                //                        },
                //                    },

                //                    Stop,

                //                    new Maneuver
                //                    {
                //                        Actions = new List<ControlAction>
                //                        {
                //                            new ControlAction {Direction = Direction.Forward, Magnitude = 10 },
                //                            new ControlAction {Direction = Direction.Up, Magnitude = -10 },
                //                            new ControlAction {Direction = Direction.Right, Magnitude = 10 },
                //                        },
                //                    },

                //                    Stop,

                //                    new Maneuver
                //                    {
                //                        Actions = new List<ControlAction>
                //                        {
                //                            new ControlAction {Direction = Direction.Forward, Magnitude = 10 },
                //                            new ControlAction {Direction = Direction.Up, Magnitude = 10 },
                //                            new ControlAction {Direction = Direction.Right, Magnitude = -10 },
                //                        },
                //                    },

                //                    Stop,

                //                    new Maneuver
                //                    {
                //                        Actions = new List<ControlAction>
                //                        {
                //                            new ControlAction {Direction = Direction.Forward, Magnitude = -10 },
                //                            new ControlAction {Direction = Direction.Up, Magnitude = -10 },
                //                            new ControlAction {Direction = Direction.Right, Magnitude = 10 },
                //                        },
                //                    },

                //                    Stop,

                //                    new Maneuver
                //                    {
                //                        Actions = new List<ControlAction>
                //                        {
                //                            new ControlAction {Direction = Direction.Forward, Magnitude = 10 },
                //                            new ControlAction {Direction = Direction.Up, Magnitude = -10 },
                //                            new ControlAction {Direction = Direction.Right, Magnitude = -10 },
                //                        },
                //                    },

                //                    Stop,

                //                    new Maneuver
                //                    {
                //                        Actions = new List<ControlAction>
                //                        {
                //                            new ControlAction {Direction = Direction.Forward, Magnitude = -10 },
                //                            new ControlAction {Direction = Direction.Up, Magnitude = 10 },
                //                            new ControlAction {Direction = Direction.Right, Magnitude = -10 },
                //                        },
                //                    },

                //                    Stop,

                //                    new Maneuver
                //                    {
                //                        Actions = new List<ControlAction>
                //                        {
                //                            new ControlAction {Direction = Direction.Forward, Magnitude = -10 },
                //                            new ControlAction {Direction = Direction.Up, Magnitude = -10 },
                //                            new ControlAction {Direction = Direction.Right, Magnitude = -10 },
                //                        },
                //                    },

                //                    Stop,
                //#endregion
                //                };

                public override bool Run() {

                    FirstRunTasks();
                    // Planetary orbit point
                    FlightController.OrbitPoint(new Vector3D(
                        -38027,
                        -39186,
                        -28549
                    ));
                    // Space orbit point
                    //FlightController.OrbitPoint(new Vector3D(
                    //    -62478,
                    //    -88117,
                    //    -55007
                    //    ));
                    return false;

                    // Use this to execute the test suite
                    //switch (phase) {

                    //    case LaunchPhase.Idle:
                    //        phase = LaunchPhase.PerformingManeuver;
                    //        CreateOkBroadcast("Starting test suite");
                    //        break;

                    //    case LaunchPhase.PerformingManeuver:
                    //        PerformManeuvers(testSequence);
                    //        break;

                    //    case LaunchPhase.Terminal:
                    //        SelfDestruct();
                    //        break;

                    //}

                    //return false;
                }

                //                private void PerformManeuvers(List<Maneuver> maneuvers) {
                //                    if (currentManeuverIndex < testSequence.Count) {
                //                        Maneuver currentManeuver = testSequence[currentManeuverIndex];
                //                        bool maneuverCompleted = PerformManeuver(currentManeuver);
                //                        if (maneuverCompleted) {
                //                            currentManeuverIndex++;
                //                        }
                //                    } else {
                //                        phase = LaunchPhase.Terminal;
                //                    }
                //                }

                //                private static bool PerformManeuver(Maneuver maneuver) {
                //                    bool allActionsCompleted = true;
                //                    foreach (var action in maneuver.Actions) {
                //                        bool actionCompleted = false;
                //                        switch (action.Direction) {
                //                            case Direction.Forward:
                //                                actionCompleted = FlightController.SetVz(action.Magnitude);
                //                                break;
                //                            case Direction.Right:
                //                                actionCompleted = FlightController.SetVx(action.Magnitude);
                //                                break;
                //                            case Direction.Up:
                //                                actionCompleted = FlightController.SetVy(action.Magnitude);
                //                                break;
                //                        }
                //                        allActionsCompleted &= actionCompleted;
                //                    }
                //                    return allActionsCompleted;
                //                }
            }
        }
    }
}
