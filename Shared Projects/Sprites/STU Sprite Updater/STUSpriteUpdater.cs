using System;

namespace IngameScript {
    partial class Program {
        public class STUSpriteUpdater {

            public Action Updater { get; set; }
            public STUSprite Sprite { get; set; }

            public STUSpriteUpdater() { }

            public STUSpriteUpdater(STUSprite sprite, Action callback) {
                Sprite = sprite;
                Updater = callback;
            }

            public void Update() {
                Updater?.Invoke();
            }

        }
    }
}
