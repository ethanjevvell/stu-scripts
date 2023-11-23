
using System;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class STULog
        {
            public enum LogType
            {
                OK,
                ERROR,
                WARNING,
            }

            public string Message { get; set; }
            public string Sender { get; set; }
            public LogType Type { get; set; }

            public STULog(string sender, string message, LogType type)
            {
                Sender = sender;
                Message = message;
                Type = type;
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

                return new STULog(components[0], components[1], logType);
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
                return $"> {Sender}: {Message}";
            }

            /// <summary>
            /// Returns a string representation of a <c>Log</c>, usually for transmission via IGC
            /// </summary>
            /// <returns>string</returns>
            public string Serialize()
            {
                return $"{Sender};{Message};{Type}";
            }
        }
    }
}
