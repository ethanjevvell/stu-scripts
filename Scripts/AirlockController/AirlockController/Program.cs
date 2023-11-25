using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Permissions;
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
    partial class Program : MyGridProgram
    {
        // Finite state machine to automatically controll airlocks
        // on the LHQ base. Future versions will be portable to 
        // an arbitrary station, given that the programmer defines
        // which doors are airlocks and which doors are related to
        // each other.

        public enum State
        {
            idle,
            entryToShip,
            entryToStation,
            closeAToShip,
            closeAToStation,
            openBToShip,
            openBToStation,
            exitToShip,
            exitToStation,
            closeBToShip, 
            closeBToStation
        }

        public State currentState;

        public List<IMyTerminalBlock> listDockAirlocks;

        public IMyDoor doorA;
        public IMyDoor doorB;

        double timeSinceLastRun = 0;
        
        public Program()
        {
            doorA = GridTerminalSystem.GetBlockWithName("Dock W2 Airlock 1") as IMyDoor;
            doorB = GridTerminalSystem.GetBlockWithName("Dock W2 Airlock 2") as IMyDoor;

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Update();
        }

        public void Update()
        {
            switch (currentState)
            {
                case State.idle:
                    HandleIdle(); break;
                case State.entryToShip:
                    HandleEntryToShip(); break;
                case State.entryToStation:
                    HandleEntryToStation(); break;
                case State.closeAToShip:
                    HandleCloseAToShip(); break;
                case State.closeAToStation:
                    HandleCloseAToStation(); break;
                case State.openBToShip: 
                    HandleOpenBToShip(); break;
                case State.openBToStation:
                    HandleOpenBToStation(); break;
                case State.exitToShip:
                    HandleExitToShip(); break;
                case State.exitToStation:
                    HandleExitToStation(); break;
                case State.closeBToShip:
                    HandleCloseBToShip(); break;
                case State.closeBToStation:
                    HandleCloseBToStation(); break;
            }
        }
              
        public void HandleIdle()
        {
            if (doorA.Status == DoorStatus.Closed && doorB.Status == DoorStatus.Closed)
            {
                currentState = State.idle;
                return;
            }

            if (doorA.Status == DoorStatus.Opening)
            {
                currentState = State.entryToShip;
                return;
            }

            if (doorB.Status == DoorStatus.Opening)
            {
                currentState = State.entryToStation;
                return;
            }
        }

        
        public void HandleEntryToShip() 
        {
            timeSinceLastRun += Runtime.TimeSinceLastRun.TotalMilliseconds;

            if (timeSinceLastRun > 3000)
            {
                currentState = State.closeAToShip;
                return;
            }
        }

        public void HandleEntryToStation() 
        {
            timeSinceLastRun += Runtime.TimeSinceLastRun.TotalMilliseconds;

            if (timeSinceLastRun > 3000)
            {
                currentState = State.closeAToStation;
                return;
            }
        }

        public void HandleCloseAToShip() 
        {
            doorA.CloseDoor();

            currentState = State.openBToShip;
            return;
        }

        public void HandleCloseAToStation() 
        {
            doorB.CloseDoor();

            currentState = State.openBToStation;
            return;
        }

        public void HandleOpenBToShip() 
        {
            doorB.OpenDoor();

            currentState = State.exitToShip;
            return;
        }

        public void HandleOpenBToStation() 
        {
            doorA.OpenDoor();

            currentState = State.exitToStation;
            return;
        }

        public void HandleExitToShip() 
        {
            timeSinceLastRun += Runtime.TimeSinceLastRun.TotalMilliseconds;

            if (timeSinceLastRun > 3000)
            {
                currentState = State.closeBToShip;
                return;
            }
        }

        public void HandleExitToStation() 
        {
            timeSinceLastRun += Runtime.TimeSinceLastRun.TotalMilliseconds;

            if (timeSinceLastRun > 3000)
            {
                currentState = State.closeBToStation;
                return;
            }
        }

        public void HandleCloseBToShip() 
        {
            doorA.CloseDoor();

            currentState = State.idle;
            return;
        }

        public void HandleCloseBToStation() 
        {
            doorB.CloseDoor();

            currentState = State.idle;
            return;
        }


    }
}
