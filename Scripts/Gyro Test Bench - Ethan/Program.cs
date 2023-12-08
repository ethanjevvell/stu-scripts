
using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        bool halt = false;

        public IMyGyro Gyro;
        public IMyRemoteControl RemoteControl;

        public Vector3D TestOne = new Vector3D(581.42, -344.59, -897.14);
        public Vector3D TestTwo = new Vector3D(593.26, -370.04, -912.82);
        public Vector3D TestThree = new Vector3D(622.44, -331.39, -910.35);
        public Vector3D TestFour = new Vector3D(595.58, -323.89, -924.78);

        public Program() {
            RemoteControl = GridTerminalSystem.GetBlockWithName("Remote Control") as IMyRemoteControl;
            Gyro = GridTerminalSystem.GetBlockWithName("Gyroscope") as IMyGyro;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        enum TestPhase {
            Start,
            One,
            Two,
            End
        }

        TestPhase testPhase = TestPhase.Start;

        public void Main(string argument) {

            switch (testPhase) {

                case TestPhase.Start:
                    Echo("Starting test phase");
                    testPhase = TestPhase.One;
                    break;

                case TestPhase.One:
                    Echo("Starting test phase");
                    if (AlignGyro(TestTwo)) {
                        testPhase = TestPhase.Two;
                    };
                    break;

                case TestPhase.Two:
                    Echo("Test phase one");
                    if (AlignGyro(TestFour)) {
                        testPhase = TestPhase.End;
                    };
                    break;

                case TestPhase.End:
                    Echo("Finished tests");
                    return;

            }

            if (argument.Contains("reset")) {
                Reset(Gyro);
            }

            if (halt) {
                Echo("Program halting...");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                Reset(Gyro);
            }
        }

        public void Reset(IMyGyro gyro) {
            Echo("resetting gyro moments to 0");
            gyro.GyroOverride = true;
            gyro.Pitch = 0;
            gyro.Roll = 0;
            gyro.Yaw = 0;
            halt = true;
        }

        public bool AlignGyro(Vector3D target) {
            Vector3D gyroPosition = Gyro.GetPosition();
            Vector3D targetPosition = target;
            Vector3D targetVector = targetPosition - gyroPosition;
            Vector3D targetVectorNormalized = Vector3D.Normalize(targetPosition - gyroPosition);

            Vector3D forwardVector = RemoteControl.WorldMatrix.Forward;
            Vector3D rotationAxis = Vector3D.Cross(forwardVector, targetVectorNormalized);
            double rotationAngle = Math.Acos(Vector3D.Dot(forwardVector, targetVectorNormalized));

            Echo(rotationAngle.ToString());

            if (Math.Abs(rotationAngle - Math.PI) < 0.01) {
                Echo("Target reached");
                return true;
            }

            Vector3D localRotationAxis = Vector3D.TransformNormal(rotationAxis, MatrixD.Transpose(RemoteControl.WorldMatrix));

            Gyro.Yaw = (float)localRotationAxis.Y;
            Gyro.Pitch = (float)localRotationAxis.X;
            Gyro.Roll = (float)localRotationAxis.Z;

            return false;
        }
    }
}
