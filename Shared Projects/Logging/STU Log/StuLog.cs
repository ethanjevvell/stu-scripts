
using System;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class STULog
        {
            public STULog() { }

            private string message;
            private string sender;
            private LogType type;

            public enum LogType
            {
                OK,
                ERROR,
                WARNING,
            }

            /// <summary>
            /// Creates a new <c>Log</c> object
            /// </summary>
            /// <param name="s"></param>
            /// <returns>A <c>Log</c> if successful, <c>null</c> otherwise</returns>
            public static STULog Deserialize(string s)
            {
                string[] components = s.Split(';');
                if (components.Length != 3)
                {
                    return null;
                }

                LogType logType;
                if (!Enum.TryParse(components[2], out logType))
                {
                    return null;
                }

                return new STULog
                {
                    Sender = components[0],
                    Message = components[1],
                    Type = logType
                };
            }

            public static Color GetColor(LogType type)
            {
                switch (type)
                {
                    case LogType.OK:
                        return Color.Green;
                    case LogType.ERROR:
                        return Color.Red;
                    case LogType.WARNING:
                        return Color.Yellow;
                    default:
                        return Color.White;
                }
            }

            /// <summary>
            /// Formats the string for output to an LCD
            /// </summary>
            /// <returns></returns>
            public string GetLogString()
            {
                return $"> {sender}: {message}";
            }

            /// <summary>
            /// Returns a string representation of a <c>Log</c>, usually for transmission via IGC
            /// </summary>
            /// <returns>string</returns>
            public string Serialize()
            {
                return $"{sender};{message};{type}";
            }

            // GETTERS AND SETTERS
            public string Message
            {
                get
                {
                    return message;
                }

                set { message = value; }
            }

            public string Sender
            {
                get
                {
                    return sender;
                }

                set { sender = value; }
            }

            public LogType Type
            {
                get
                {
                    return type;
                }
                set { type = value; }
            }

        }
    }
}
