using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        IMyTerminalBlock BaseNode;
        IMyShipController ControlSeat;
        MainDisplay PBDisplay;

        public Program() {
            BaseNode = Me;
            PBDisplay = new MainDisplay(BaseNode, 0, Echo);
            ControlSeat = GridTerminalSystem.GetBlockWithName("OBSERVER") as IMyShipController;

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main() {
            PBDisplay.Update(ControlSeat);
        }

        public partial class MainDisplay : STUDisplay {

            Action<MySpriteDrawFrame, Vector2, float> Drawer;
            STUDisplayDrawMapper MainDisplayMap = new STUDisplayDrawMapper {
                DisplayDrawMapper = {
                    {
                        STUDisplayType.CreateDisplayIdentifier(STUDisplayBlock.LargeProgrammableBlock, STUSubDisplay.LargeDisplay), LargeProgrammableBlock.LargeScreen },
                    }
            };

            public static Vector3D BodyGravityVector = new Vector3D();
            public static Vector3D WorldGravityVector = new Vector3D();
            public static Vector3D ObserverWorldPosition = new Vector3D();
            public static double BodyGravityMagnitude = 0;
            public static double WorldGravityMagniutde = 0;
            public Action<string> Echo;

            public MainDisplay(IMyTerminalBlock block, int displayIndex, Action<string> echo) : base(block, displayIndex) {
                Drawer = MainDisplayMap.GetDrawFunction(block, displayIndex);
                Echo = echo;
            }

            public void CalculateMagnitude() {
                // Distance formula in 3D
                WorldGravityMagniutde = Math.Sqrt((WorldGravityVector.X * WorldGravityVector.X) + (WorldGravityVector.Y * WorldGravityVector.Y) + (WorldGravityVector.Z * WorldGravityVector.Z));
                BodyGravityMagnitude = Math.Sqrt((BodyGravityVector.X * BodyGravityVector.X) + (BodyGravityVector.Y * BodyGravityVector.Y) + (BodyGravityVector.Z * BodyGravityVector.Z));

                Echo($"G_w_v: \n{WorldGravityVector}");
                Echo($"G_b_v: \n{BodyGravityVector}");
                Echo($"W_p: \n {ObserverWorldPosition}");
            }

            public void Draw() {
                StartFrame();
                Drawer.Invoke(CurrentFrame, Viewport.Center, 1f);
                EndAndPaintFrame();
            }

            public void Update(IMyShipController observer) {
                BodyGravityVector = observer.GetNaturalGravity();
                WorldGravityVector = Vector3D.TransformNormal(BodyGravityVector, observer.WorldMatrix);
                ObserverWorldPosition = observer.GetPosition();
                CalculateMagnitude();
                Draw();
            }

        }
    }

}
