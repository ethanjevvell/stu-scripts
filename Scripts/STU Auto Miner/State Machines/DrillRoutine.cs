using Sandbox.ModAPI.Ingame;
using System;
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
            List<IMyShipDrill> Drills { get; set; }
            Vector3 JobSite { get; set; }
            PlaneD JobPlane { get; set; }

            class Silo {
                public Vector3D StartPos;
                public Vector3D EndPos;
                public Silo(Vector3D startPos, Vector3D endPos) {
                    StartPos = startPos;
                    EndPos = endPos;
                }
            }

            List<List<Silo>> Silos { get; set; }

            public override bool Init() {
                FlightController.ReinstateGyroControl();
                FlightController.ReinstateThrusterControl();
                Drills.ForEach(drill => drill.Enabled = true);
                return true;
            }

            public override bool Closeout() {
                throw new Exception("DrillRoutine.Closeout() not implemented");
            }

            public DrillRoutine(STUFlightController fc, List<IMyShipDrill> drills, List<IMyGasTank> hydrogenTanks, List<IMyBatteryBlock> batteries, Vector3 jobSite, PlaneD jobPlane) {

                FlightController = fc;
                HydrogenTanks = hydrogenTanks;
                Batteries = batteries;
                RunState = RunStates.ORIENT_AGAINST_JOB_PLANE;
                JobSite = jobSite;
                JobPlane = jobPlane;
                Drills = drills;

                if (JobPlane == null) {
                    throw new Exception("Job plane is null");
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
                        FlightController.SetStableForwardVelocity(0);
                        if (FlightController.AlignShipToTarget(closestPointOnJobPlane)) {
                            CreateInfoBroadcast("Oriented against job plane");
                            Silos = GetSilos(JobSite, JobPlane, 3);
                            for (int i = 0; i < Silos.Count; i++) {
                                for (int j = 0; j < Silos.Count; j++) {
                                    CreateInfoBroadcast($"S[{i}][{j}] start: {Silos[i][j].StartPos}");
                                    CreateInfoBroadcast($"S[{i}][{j}] end: {Silos[i][j].EndPos}");
                                }
                            }
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


            private List<List<Silo>> GetSilos(Vector3D jobSite, PlaneD jobPlane, int n) {
                // Ensure Drills are initialized and contain elements
                if (Drills == null || Drills.Count == 0) {
                    throw new Exception("Drills list is empty or uninitialized.");
                }

                double shipWidth = Drills[0].CubeGrid.WorldAABB.Size.X;
                double shipHeight = Drills[0].CubeGrid.WorldAABB.Size.Y;
                double shipLength = Drills[0].CubeGrid.WorldAABB.Size.Z;

                // Get the normal of the job plane
                Vector3D normal = Vector3D.Normalize(jobPlane.Normal);

                // Handle the case where normal is parallel to Vector3D.Up
                Vector3D upVector = Vector3D.Up;
                if (Vector3D.IsZero(Vector3D.Cross(normal, upVector))) {
                    upVector = Vector3D.Right; // Use an alternative vector
                }
                Vector3D right = Vector3D.Normalize(Vector3D.Cross(normal, upVector));
                Vector3D up = Vector3D.Normalize(Vector3D.Cross(right, normal));

                double halfGridSize = (n - 1) / 2.0;
                double siloDepth = shipLength * 2; // Adjust as needed

                List<List<Silo>> silos = new List<List<Silo>>();
                for (int i = 0; i < n; i++) {
                    List<Silo> row = new List<Silo>();
                    for (int j = 0; j < n; j++) {
                        Vector3D offset = right * (i - halfGridSize) * shipWidth + up * (j - halfGridSize) * shipHeight;
                        Vector3D startPos = jobSite + offset;
                        Vector3D endPos = startPos + normal * siloDepth;
                        row.Add(new Silo(startPos, endPos));
                    }
                    silos.Add(row);
                }

                return silos;
            }
        }
    }
}
