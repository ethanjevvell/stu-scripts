using VRage.Game.GUI.TextPanel;

namespace IngameScript {
    partial class Program {
        /// <summary>
        /// Simple wrapper for MySprite so that we can modify sprites in-place.
        /// </summary>
        public class STUSprite {

            public MySprite Sprite { get; set; }

            public STUSprite(MySprite sprite) {
                Sprite = sprite;
            }

        }

    }
}
