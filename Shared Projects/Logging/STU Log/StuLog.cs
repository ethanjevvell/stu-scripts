
namespace IngameScript
{
    partial class Program
    {
        public class StuLog
        {
            public StuLog() { }

            private string message;
            private string sender;

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

            /// <summary>
            /// Returns <c>null</c> if <c>s</c> could not be converted to Log
            /// </summary>
            /// <param name="s"></param>
            /// <returns></returns>
            public static StuLog Deserialize(string s)
            {
                string[] components = s.Split(';');
                if (components.Length != 2)
                {
                    return null;
                }
                return new StuLog
                {
                    message = components[0],
                    sender = components[1]
                };
            }

            /// <summary>
            /// Returns a string representation of a Log, usually for transmission via IGC
            /// </summary>
            /// <returns></returns>
            public string Serialize()
            {
                return $"{sender};{message}";
            }

        }
    }
}
