
using System;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public enum STULogType {
            OK,
            ERROR,
            WARNING,
        }

        public class STULog {

            private string message;
            private string sender;
            private STULogType type;

            public STULog(string sender, string message, STULogType type) {
                Sender = sender;
                Message = message;
                Type = type;
            }

            /// <summary>
            /// Creates a new <c>Log</c> object
            /// </summary>
            /// <param name="s"></param>
            /// <returns>A <c>Log</c> if successful, <c>null</c> otherwise</returns>
            public static STULog Deserialize(string s) {
                string[] components = s.Split(';');
                if (components.Length != 3) {
                    return null;
                }

                STULogType logType;
                if (!Enum.TryParse(components[2], out logType)) {
                    return null;
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
            /// Formats the string for output to an LCD
            /// </summary>
            /// <returns></returns>
            public string GetLogString() {
                return $"> {sender}: {message}";
            }

            /// <summary>
            /// Returns a string representation of a <c>Log</c>, usually for transmission via IGC
            /// </summary>
            /// <returns>string</returns>
            public string Serialize() {
                return $"{sender};{message};{type}";
            }


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
