using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript {
    partial class Program {
        public partial class Missile {

            public class PID {
                public double Kp { get; set; } = 0;
                public double Ki { get; set; } = 0;
                public double Kd { get; set; } = 0;
                public double Value { get; private set; }

                double TimeStep = 0;
                double InverseTimeStep = 0;
                double ErrorSum = 0;
                double LastError = 0;
                bool FirstRun = true;

                public PID(double kp, double ki, double kd, double timeStep) {
                    Kp = kp;
                    Ki = ki;
                    Kd = kd;
                    TimeStep = timeStep;
                    InverseTimeStep = 1 / TimeStep;
                }

                protected virtual double GetIntegral(double currentError, double errorSum, double timeStep) {
                    return errorSum + currentError * timeStep;
                }

                public double Control(double error) {
                    //Compute derivative term
                    double errorDerivative = (error - LastError) * InverseTimeStep;

                    if (FirstRun) {
                        errorDerivative = 0;
                        FirstRun = false;
                    }

                    //Get error sum
                    ErrorSum = GetIntegral(error, ErrorSum, TimeStep);

                    //Store this error as last error
                    LastError = error;

                    //Construct output
                    Value = Kp * error + Ki * ErrorSum + Kd * errorDerivative;
                    return Value;
                }

                public double Control(double error, double timeStep) {
                    if (timeStep != TimeStep) {
                        TimeStep = timeStep;
                        InverseTimeStep = 1 / TimeStep;
                    }
                    return Control(error);
                }

                public virtual void Reset() {
                    ErrorSum = 0;
                    LastError = 0;
                    FirstRun = true;
                }
            }

            public class DecayingIntegralPID : PID {
                public double IntegralDecayRatio { get; set; }

                public DecayingIntegralPID(double kp, double ki, double kd, double timeStep, double decayRatio) : base(kp, ki, kd, timeStep) {
                    IntegralDecayRatio = decayRatio;
                }

                protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                    return errorSum * (1.0 - IntegralDecayRatio) + currentError * timeStep;
                }
            }

            public class ClampedIntegralPID : PID {
                public double IntegralUpperBound { get; set; }
                public double IntegralLowerBound { get; set; }

                public ClampedIntegralPID(double kp, double ki, double kd, double timeStep, double lowerBound, double upperBound) : base(kp, ki, kd, timeStep) {
                    IntegralUpperBound = upperBound;
                    IntegralLowerBound = lowerBound;
                }

                protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                    errorSum = errorSum + currentError * timeStep;
                    return Math.Min(IntegralUpperBound, Math.Max(errorSum, IntegralLowerBound));
                }
            }

            public class BufferedIntegralPID : PID {
                readonly Queue<double> _integralBuffer = new Queue<double>();
                public int IntegralBufferSize { get; set; } = 0;

                public BufferedIntegralPID(double kp, double ki, double kd, double timeStep, int bufferSize) : base(kp, ki, kd, timeStep) {
                    IntegralBufferSize = bufferSize;
                }

                protected override double GetIntegral(double currentError, double errorSum, double timeStep) {
                    if (_integralBuffer.Count == IntegralBufferSize)
                        _integralBuffer.Dequeue();
                    _integralBuffer.Enqueue(currentError * timeStep);
                    return _integralBuffer.Sum();
                }

                public override void Reset() {
                    base.Reset();
                    _integralBuffer.Clear();
                }
            }
        }
    }
}
