using System.Collections.Generic;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public class TestSuite {

                public enum LaunchPhase {
                    Idle,
                    PerformingManeuver,
                    Terminal
                }

                public enum Direction {
                    Forward,
                    Right,
                    Up
                }

                public struct ControlAction {
                    public Direction Direction;
                    public float Magnitude;
                }

                public struct Maneuver {
                    public List<ControlAction> Actions;
                }

                public static LaunchPhase phase = LaunchPhase.Idle;
                public static int currentManeuverIndex = 0;

                private static Maneuver Stop = new Maneuver {
                    Actions = new List<ControlAction> {
                        new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                        new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                        new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                    }
                };

                public static List<Maneuver> testSequence = new List<Maneuver> {

                    // Single-axis tests
                    #region
                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = 5 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = -5 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = -50 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    Stop,


                    // another set of single-axis tests for right
                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Right, Magnitude = 5 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Right, Magnitude = -5 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Right, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Right, Magnitude = -50 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    Stop,

                    // now for up

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Up, Magnitude = 5 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Up, Magnitude = -5 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Up, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Up, Magnitude = -50 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                        }
                    },

                    Stop,
                    #endregion

                    // Two-axis tests
                    #region
                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = 1 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 5 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = -1 },
                             new ControlAction { Direction = Direction.Right, Magnitude = -5 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = -50 },
                             new ControlAction { Direction = Direction.Right, Magnitude = -50 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 0 }
                        }
                    },

                    Stop,

                    // now for forward and up

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = 1 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 5 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = -1 },
                             new ControlAction { Direction = Direction.Up, Magnitude = -5 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Forward, Magnitude = -50 },
                             new ControlAction { Direction = Direction.Up, Magnitude = -50 },
                             new ControlAction { Direction = Direction.Right, Magnitude = 0 }
                        }
                    },

                    Stop,

                    // now for right and up

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Right, Magnitude = 1 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 5 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Right, Magnitude = -1 },
                             new ControlAction { Direction = Direction.Up, Magnitude = -5 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Right, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Up, Magnitude = 50 },
                             new ControlAction { Direction = Direction.Forward, Magnitude = 0 }
                        }
                    },

                    new Maneuver {
                        Actions = new List<ControlAction> {
                             new ControlAction { Direction = Direction.Right, Magnitude = -50 },
                             new ControlAction { Direction = Direction.Up, Magnitude = -50 },
                             new ControlAction {Direction = Direction.Forward, Magnitude = 0},
                        }
                    },

                    Stop,
                    #endregion

                    // octant tests
                    #region
                    new Maneuver
                    {
                        Actions = new List<ControlAction>
                        {
                            new ControlAction {Direction = Direction.Forward, Magnitude = 10 },
                            new ControlAction {Direction = Direction.Up, Magnitude = 10 },
                            new ControlAction {Direction = Direction.Right, Magnitude = 10 },
                        },
                    },

                    Stop,

                    new Maneuver
                    {
                        Actions = new List<ControlAction>
                        {
                            new ControlAction {Direction = Direction.Forward, Magnitude = -10 },
                            new ControlAction {Direction = Direction.Up, Magnitude = 10 },
                            new ControlAction {Direction = Direction.Right, Magnitude = 10 },
                        },
                    },

                    Stop,

                    new Maneuver
                    {
                        Actions = new List<ControlAction>
                        {
                            new ControlAction {Direction = Direction.Forward, Magnitude = 10 },
                            new ControlAction {Direction = Direction.Up, Magnitude = -10 },
                            new ControlAction {Direction = Direction.Right, Magnitude = 10 },
                        },
                    },

                    Stop,

                    new Maneuver
                    {
                        Actions = new List<ControlAction>
                        {
                            new ControlAction {Direction = Direction.Forward, Magnitude = 10 },
                            new ControlAction {Direction = Direction.Up, Magnitude = 10 },
                            new ControlAction {Direction = Direction.Right, Magnitude = -10 },
                        },
                    },

                    Stop,

                    new Maneuver
                    {
                        Actions = new List<ControlAction>
                        {
                            new ControlAction {Direction = Direction.Forward, Magnitude = -10 },
                            new ControlAction {Direction = Direction.Up, Magnitude = -10 },
                            new ControlAction {Direction = Direction.Right, Magnitude = 10 },
                        },
                    },

                    Stop,

                    new Maneuver
                    {
                        Actions = new List<ControlAction>
                        {
                            new ControlAction {Direction = Direction.Forward, Magnitude = 10 },
                            new ControlAction {Direction = Direction.Up, Magnitude = -10 },
                            new ControlAction {Direction = Direction.Right, Magnitude = -10 },
                        },
                    },

                    Stop,

                    new Maneuver
                    {
                        Actions = new List<ControlAction>
                        {
                            new ControlAction {Direction = Direction.Forward, Magnitude = -10 },
                            new ControlAction {Direction = Direction.Up, Magnitude = 10 },
                            new ControlAction {Direction = Direction.Right, Magnitude = -10 },
                        },
                    },

                    Stop,

                    new Maneuver
                    {
                        Actions = new List<ControlAction>
                        {
                            new ControlAction {Direction = Direction.Forward, Magnitude = -10 },
                            new ControlAction {Direction = Direction.Up, Magnitude = -10 },
                            new ControlAction {Direction = Direction.Right, Magnitude = -10 },
                        },
                    },

                    Stop,
#endregion
                };

                public static void Run() {
                    switch (phase) {
                        case LaunchPhase.Idle:
                            phase = LaunchPhase.PerformingManeuver;
                            break;

                        case LaunchPhase.PerformingManeuver:
                            if (currentManeuverIndex < testSequence.Count) {
                                Maneuver currentManeuver = testSequence[currentManeuverIndex];
                                bool maneuverCompleted = PerformManeuver(currentManeuver);
                                if (maneuverCompleted) {
                                    currentManeuverIndex++;
                                }
                            } else {
                                phase = LaunchPhase.Terminal;
                            }
                            break;

                        case LaunchPhase.Terminal:
                            SelfDestruct();
                            break;
                    }
                }

                private static bool PerformManeuver(Maneuver maneuver) {
                    bool allActionsCompleted = true;
                    foreach (var action in maneuver.Actions) {
                        bool actionCompleted = false;
                        switch (action.Direction) {
                            case Direction.Forward:
                                actionCompleted = Maneuvers.Velocity.ControlForward(action.Magnitude);
                                break;
                            case Direction.Right:
                                actionCompleted = Maneuvers.Velocity.ControlRight(action.Magnitude);
                                break;
                            case Direction.Up:
                                actionCompleted = Maneuvers.Velocity.ControlUp(action.Magnitude);
                                break;
                        }
                        allActionsCompleted &= actionCompleted;
                    }
                    return allActionsCompleted;
                }
            }
        }
    }
}
