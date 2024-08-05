
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public static class STULogType {
            public const string OK = "OK";
            public const string ERROR = "ERROR";
            public const string WARNING = "WARNING";
            public const string INFO = "INFO";
        }

        /// <summary>
        /// A custom Log object for use with the STU master logging system.
        /// All three fields are required to be defind and non-empty to be valid.
        /// </summary>
        public class STULog {

            private string message;
            private string sender;
            private string type;
            private Dictionary<string, string> metadata;

            private const string COMPONENT_DELIMITER = "\x1F";
            private const string METADATA_DELIMITER = "\x1E";

            public STULog() { }

            public STULog(string sender, string message, string type, Dictionary<string, string> metadata = null) {
                Sender = sender;
                Message = message;
                Type = type;
                Metadata = metadata;
            }

            /// <summary>
            /// Creates a new <c>STULog</c> object. Be sure to surround in try-catch.
            /// </summary>
            /// <param name="s"></param>
            /// <returns><c>STULog</c></returns>
            /// <exception cref="ArgumentException">Thrown if deserialization fails.</exception>"
            public static STULog Deserialize(string s) {

                string[] components = s.Split(new string[] { COMPONENT_DELIMITER }, StringSplitOptions.None);

                string logType = components[2];
                switch (components.Length) {
                    case 3:
                        // No metadata
                        return new STULog(components[0], components[1], logType);
                    case 4:
                        // Contains metadata
                        return new STULog(components[0], components[1], logType, ParseMetadata(components[3]));
                    default:
                        // Any other number of components is invalid
                        throw new ArgumentException($"Malformed log string; invalid number of components: {components.Length}.");
                }

            }

            private static Dictionary<string, string> ParseMetadata(string metadataString) {
                Dictionary<string, string> metadata = new Dictionary<string, string>();
                string[] metadataKeyValuePairs = metadataString.Split(new string[] { METADATA_DELIMITER }, StringSplitOptions.None);

                foreach (string pair in metadataKeyValuePairs) {
                    string[] keyValue = pair.Split('=');
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();
                    metadata.Add(key, value);
                }
                return metadata;
            }

            public static Color GetColor(string type) {
                switch (type) {
                    case STULogType.OK:
                        return Color.Green;
                    case STULogType.ERROR:
                        return Color.Red;
                    case STULogType.WARNING:
                        return Color.Yellow;
                    case STULogType.INFO:
                        return Color.White;
                    default:
                        return Color.White;
                }
            }

            /// <summary>
            /// Returns a string representation of a <c>Log</c>, usually for transmission via IGC
            /// </summary>
            /// <returns>string</returns>
            public string Serialize() {

                if (Metadata == null) {
                    return string.Join(COMPONENT_DELIMITER, Sender, Message, Type);
                }

                string[] keyPairStrings = new string[Metadata.Keys.Count];
                int i = 0;
                foreach (KeyValuePair<string, string> pair in Metadata) {
                    keyPairStrings[i] = $"{pair.Key}={pair.Value}";
                    i++;
                }
                return string.Join(COMPONENT_DELIMITER, Sender, Message, Type, string.Join(METADATA_DELIMITER, keyPairStrings));
            }

            // GETTERS AND SETTERS // 

            public string Sender {
                get {
                    return sender;
                }
                set {
                    if (string.IsNullOrEmpty(value)) {
                        throw new ArgumentException("Sender cannot be an empty string");
                    } else {
                        sender = value;
                    }
                }
            }

            public string Message {
                get {
                    return message;
                }
                set {
                    message = value;
                }
            }

            public string Type {
                get {
                    return type;
                }
                set {
                    type = value;
                }
            }

            public Dictionary<string, string> Metadata {
                get {
                    return metadata;
                }
                set {
                    metadata = value;
                }
            }

        }

    }
}

