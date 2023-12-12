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

            public static Vector3D WorldGravityVector = new Vector3D();
            public static Vector3D LocalGravityVector = new Vector3D();
            public static Vector3D ObserverWorldPosition = new Vector3D();
            public static double WorldGravityMagnitude = 0;
            public static double LocalGravityMagnitude = 0;
            public Action<string> Echo;

            public MainDisplay(IMyTerminalBlock block, int displayIndex, Action<string> echo) : base(block, displayIndex) {
                Drawer = MainDisplayMap.GetDrawFunction(block, displayIndex);
                Echo = echo;
            }

            public void CalculateMagnitude() {
                // Distance formula in 3D
                WorldGravityMagnitude = WorldGravityVector.Length();
                LocalGravityMagnitude = LocalGravityVector.Length();
                Echo($"G_w_v: \n{WorldGravityVector}");
                Echo($"G_l_v: \n{LocalGravityVector}");
                Echo($"W_p: \n {ObserverWorldPosition}");
            }

            public void Draw() {
                StartFrame();
                Drawer.Invoke(CurrentFrame, Viewport.Center, 1f);
                EndAndPaintFrame();
            }

            public void Update(IMyShipController observer) {
                // IMPORTANT: .GetNaturalGravity() returns a Vector3D in terms of the world's coordinate system, not the observer's.
                WorldGravityVector = observer.GetNaturalGravity();
                LocalGravityVector = Vector3D.TransformNormal(WorldGravityVector, observer.Orientation);
                ObserverWorldPosition = observer.GetPosition();
                CalculateMagnitude();
                Draw();
            }

        }
    }

}
