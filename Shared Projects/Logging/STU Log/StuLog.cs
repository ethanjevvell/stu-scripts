
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public enum STULogType {
            OK,
            ERROR,
            WARNING,
        }

        /// <summary>
        /// A custom Log object for use with the STU master logging system.
        /// All three fields are required to be defind and non-empty to be valid.
        /// </summary>
        public class STULog {

            private string message;
            private string sender;
            private STULogType type;

            public STULog() { }

            public STULog(string sender, string message, STULogType type) {
                Sender = sender;
                Message = message;
                Type = type;
            }

            /// <summary>
            /// Creates a new <c>STULog</c> object. Be sure to surround in try-catch.
            /// </summary>
            /// <param name="s"></param>
            /// <returns><c>STULog</c></returns>
            /// <exception cref="ArgumentException">Thrown if deserialization fails.</exception>"
            public static STULog Deserialize(string s) {
                string[] components = s.Split(';');
                if (components.Length != 3) {
                    throw new ArgumentException("Malformed log string; wrong number of parsed elements.");
                }

                STULogType logType;
                if (!Enum.TryParse(components[2], out logType)) {
                    throw new ArgumentException("Malformed log string; LogType given is not valid.");
                }

                return new STULog(components[0], components[1], logType);
            }

            public static Color GetColor(STULogType type) {
                switch (type) {
                    case STULogType.OK:
                        return Color.Green;
                    case STULogType.ERROR:
                        return Color.Red;
                    case STULogType.WARNING:
                        return Color.Yellow;
                    default:
                        return Color.White;
                }
            }

            /// <summary>
            /// Returns a string representation of a <c>Log</c>, usually for transmission via IGC
            /// </summary>
            /// <returns>string</returns>
            public string Serialize() {
                return $"{Sender};{Message};{Type}";
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
                    if (string.IsNullOrEmpty(value)) {
                        throw new ArgumentException("Message cannot be an empty string");
                    } else {
                        message = value;
                    }
                }
            }

            public STULogType Type {
                get {
                    return type;
                }
                set {
                    type = value;
                }
            }

        }

    }
}
