using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public partial class DrillRoutine : STUStateMachine {

            public override string Name => "Drill Routine";

            public enum RunStates {
                ORIENT_AGAINST_JOB_PLANE,
                EXTRACT_SEGMENT,
                PULL_OUT,
                RTB
            }

            STUFlightController FlightController { get; set; }
            List<IMyGasTank> HydrogenTanks { get; set; }
            List<IMyBatteryBlock> Batteries { get; set; }
            RunStates RunState { get; set; }
            IMyShipDrill Drill { get; set; }
            Vector3 JobSite { get; set; }
            PlaneD JobPlane { get; set; }

            public override bool Init() {
                FlightController.ReinstateGyroControl();
                FlightController.ReinstateThrusterControl();
                Drill.Enabled = true;
                return true;
            }

            public override bool Closeout() {
                throw new System.Exception("DrillRoutine.Closeout() not implemented");
            }

            public DrillRoutine(STUFlightController fc, IMyShipDrill drill, List<IMyGasTank> hydrogenTanks, List<IMyBatteryBlock> batteries, Vector3 jobSite, PlaneD jobPlane) {
                FlightController = fc;
                HydrogenTanks = hydrogenTanks;
                Batteries = batteries;
                RunState = RunStates.ORIENT_AGAINST_JOB_PLANE;
                JobSite = jobSite;
                JobPlane = jobPlane;
                Drill = drill;

                if (JobPlane == null) {
                    throw new System.Exception("Job plane is null");
                }
            }

            public override bool Run() {

                if (HydrogenRunningLow()) {
                    RunState = RunStates.RTB;
                }

                if (BatteriesRunningLow()) {
                    RunState = RunStates.RTB;
                }

                switch (RunState) {

                    case RunStates.ORIENT_AGAINST_JOB_PLANE:
                        Vector3D closestPointOnJobPlane = GetClosestPointOnJobPlane(JobPlane, FlightController.CurrentPosition);
                        if (FlightController.AlignShipToTarget(closestPointOnJobPlane)) {
                            CreateInfoBroadcast($"Plane normal: {JobPlane.Normal}");
                            CreateInfoBroadcast($"Closest point: {closestPointOnJobPlane}");
                            CreateInfoBroadcast("Oriented against job plane");
                            return true;
                        }
                        break;

                    case RunStates.RTB:
                        return true;

                }

                return false;

            }

            private bool HydrogenRunningLow() {
                // figure out how much hydrogen is safe to have left
                return false;
            }

            private bool BatteriesRunningLow() {
                // figure out how much battery is safe to have left
                return false;
            }

            Vector3D GetClosestPointOnJobPlane(PlaneD jobPlane, Vector3D currentPos) {
                return jobPlane.ProjectPoint(ref currentPos);
            }

        }
    }
}
