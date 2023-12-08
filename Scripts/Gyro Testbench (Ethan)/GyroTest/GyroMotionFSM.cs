using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class GyroMotionFSM
        {
            public enum State
            {
                idle,
                roll,
                pitch,
                yaw,
                done
            }

            public State currentState;

            public IMyGyro Gyro;

            public Vector3D TargetVector;

            public GyroMotionFSM(IMyGyro gyro, Vector3D targetVector)
            {
                Gyro = gyro;
                currentState = State.idle;
                TargetVector = targetVector;
            }

            public void Update()
            {
                switch (currentState)
                {
                    case State.idle:
                        HandleIdle(); break;
                    case State.roll:
                        HandleRoll(); break;
                    case State.pitch:
                        HandlePitch(); break;
                    case State.yaw:
                        HandleYaw(); break;
                    case State.done:
                        HandleDone(); break;
                }
            }

            public void HandleIdle()
            {
                if (TargetVector != new Vector3D())
                {
                    currentState = State.roll;
                    return;
                }
            }

            public void HandleRoll()
            {

            }

            public void HandlePitch()
            {

            }

            public void HandleYaw()
            {

            }

            public void HandleDone()
            {
                Gyro.Roll = 0;
                Gyro.Pitch = 0;
                Gyro.Yaw = 0;
                TargetVector = new Vector3D();
                currentState = State.done;
            }
        }
    }
}
