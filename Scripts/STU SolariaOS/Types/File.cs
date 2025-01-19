using System;

namespace IngameScript {
    abstract class File {

        public string Name { get; protected set; }
        public string Extension { get; protected set; }
        public string Owner { get; protected set; }
        public Data Content { get; protected set; }
        public DateTime Created { get; protected set; }
        public DateTime Modified { get; protected set; }

        public File(string name, string extension, Data content) {
            Name = name;
            Extension = extension;
            Content = content;
        }

    }
}
