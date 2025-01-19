using System.Collections.Generic;

namespace IngameScript {
    internal class Directory {
        public List<File> Files { get; protected set; }

        public Directory(List<File> files) {
            Files = files;
        }
    }
}
