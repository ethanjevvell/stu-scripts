
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public class PID {
                public double Kp { get; set; } = 0;
                public double Ki { get; set; } = 0;
                public double Kd { get; set; } = 0;
                public double Value { get; private set; }

                double _timeStep = 0;
                double _inverseTimeStep = 0;
                double _errorSum = 0;
                double _lastError = 0;
                bool _firstRun = true;

                public PID(double kp, double ki, double kd, double timeStep) {
                    Kp = kp;
                    Ki = ki;
                    Kd = kd;
                    _timeStep = timeStep;
                    _inverseTimeStep = 1 / _timeStep;
                }

                protected virtual double GetIntegral(double currentError, double errorSum, double timeStep) {
                    return errorSum + currentError * timeStep;
                }

                public double Control(double error) {
                    //Compute derivative term
                    double errorDerivative = (error - _lastError) * _inverseTimeStep;

                    if (_firstRun) {
                        errorDerivative = 0;
                        _firstRun = false;
                    }

                    //Get error sum
                    _errorSum = GetIntegral(error, _errorSum, _timeStep);

                    //Store this error as last error
                    _lastError = error;

                    //Construct output
                    Value = Kp * error + Ki * _errorSum + Kd * errorDerivative;
                    return Value;
                }

                public double Control(double error, double timeStep) {
                    if (timeStep != _timeStep) {
                        _timeStep = timeStep;
                        _inverseTimeStep = 1 / _timeStep;
                    }
                    return Control(error);
                }

                public virtual void Reset() {
                    _errorSum = 0;
                    _lastError = 0;
                    _firstRun = true;
                }
            }

            public class Launch {

                // Temporary; for ensuring missile is far enough from test site before self destruct
                private const double SELF_DESTRUCT_THRESHOLD = 1000;

                private static PID InitialBurnPID = new PID(0.1, 0, 0, 1);
                private static float InitialBurnTargetVelocity = 70;

                public enum LaunchPhase {
                    Idle,
                    InitialBurn,
                    Terminal
                }

                public static LaunchPhase phase = LaunchPhase.Idle;

                public static void Run() {

                    switch (phase) {

                        case LaunchPhase.Idle:
                            phase = LaunchPhase.InitialBurn;
                            break;

                        case LaunchPhase.InitialBurn:
                            InitialBurn();
                            var distance = Vector3D.Distance(StartPosition, CurrentPosition);
                            if (distance > SELF_DESTRUCT_THRESHOLD) {
                                phase = LaunchPhase.Terminal;
                            }
                            break;

                        case LaunchPhase.Terminal:
                            SelfDestruct();
                            break;

                    }

                }

                public static void InitialBurn() {
                    var error = InitialBurnTargetVelocity - Velocity;
                    Array.ForEach(Thrusters, thruster => thruster.Thruster.ThrustOverride = thruster.Thruster.MaxThrust);
                    //Array.ForEach(Thrusters, thruster => thruster.Thruster.ThrustOverride = (float)InitialBurnPID.Control(error));
                }

            }
        }
    }
}
