using Sandbox.ModAPI.Ingame;
using System;
using System.Text;

namespace IngameScript {
    partial class Program {
        public class STUMasterLogBroadcaster {
            public string Channel { get; set; }
            public IMyIntergridCommunicationSystem Broadcaster { get; set; }
            public TransmissionDistance Distance { get; set; }

            public STUMasterLogBroadcaster(string channel, IMyIntergridCommunicationSystem IGC, TransmissionDistance distance) {
                Channel = channel;
                Broadcaster = IGC;
                Distance = distance;
            }

            /// <summary>
            /// Broadcasts a log message to the channel specified in the constructor.
            /// </summary>
            /// <param name="log">An STULog object</param>
            /// <param name="key">Optional: a passphrase to be used in the Tiny Encryption Algorithm.
            /// `key` must be at least 16 characters long.
            /// Your solution must have the CUCKS project loaded</param>
            public void Log(STULog log, string key = null) {
                string export = log.Serialize();
                if (key != null && key.Length >= 16) {
                    try {
                        export = Encrypt(log.Serialize(), key);
                    } catch {
                        throw new Exception($"TEA encryption unsuccessful: STUMasterLogBroadcaster.Log(STULog {log}, string {key}");
                    }
                }
                Broadcaster.SendBroadcastMessage(Channel, export, Distance);
            }

            public void Ping() {
                Broadcaster.SendBroadcastMessage(Channel, "PING", Distance);
            }

            #region Tiny Encryption Algorithm (TEA) Methods
            public void encode(uint[] v, uint[] k) {
                uint y = v[0];
                uint z = v[1];
                uint sum = 0;
                uint delta = 0x9e3779b9;
                uint n = 32;

                while (n-- > 0) {
                    y += (z << 4 ^ z >> 5) + z ^ sum + k[sum & 3];
                    sum += delta;
                    z += (y << 4 ^ y >> 5) + y ^ sum + k[sum >> 11 & 3];
                }

                v[0] = y;
                v[1] = z;
            }

            public void decode(uint[] v, uint[] k) {
                uint n = 32;
                uint y = v[0];
                uint z = v[1];
                uint delta = 0x9e3779b9;
                uint sum = delta << 5;

                while (n-- > 0) {
                    z -= (y << 4 ^ y >> 5) + y ^ sum + k[sum >> 11 & 3];
                    y -= (z << 4 ^ z >> 5) + z ^ sum + k[sum & 3];
                    sum -= delta;
                }

                v[0] = y;
                v[1] = z;
            }

            // totally auto generated, not sure if it works
            public uint[] FormatKey(string key) {
                uint[] formattedKey = new uint[4];
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);

                for (int i = 0; i < 4; i++) {
                    formattedKey[i] = BitConverter.ToUInt32(keyBytes, i * 4);
                }

                return formattedKey;
            }

            public string Encrypt(string data, string key) {
                uint[] formattedKey = FormatKey(key);

                if (data.Length % 2 != 0)
                    data += '\0'; // Make sure array is even length
                byte[] dataBytes = Encoding.ASCII.GetBytes(data);

                string cipher = string.Empty;
                uint[] tempData = new uint[2];
                for (int i = 0; i < dataBytes.Length; i += 2) {
                    tempData[0] = dataBytes[i];
                    tempData[1] = dataBytes[i + 1];
                    encode(tempData, formattedKey);
                    cipher += ConvertUIntToString(tempData[0]) + ConvertUIntToString(tempData[1]);
                }

                return cipher;
            }

            public string Decrypt(string data, string key) {
                uint[] formattedKey = FormatKey(key);

                int x = 0;
                uint[] tempData = new uint[2];
                byte[] dataBytes = new byte[data.Length / 8 * 2];
                for (int i = 0; i < data.Length; i += 8) {
                    tempData[0] = ConvertStringToUInt(data.Substring(i, 4));
                    tempData[1] = ConvertStringToUInt(data.Substring(i + 4, 4));
                    decode(tempData, formattedKey);
                    dataBytes[x++] = (byte)tempData[0];
                    dataBytes[x++] = (byte)tempData[1];
                }

                string decipheredString = Encoding.ASCII.GetString(dataBytes, 0, dataBytes.Length);

                // Strip null characters
                if (decipheredString[decipheredString.Length - 1] == '\0') {
                    decipheredString = decipheredString.Substring(0, decipheredString.Length - 1);
                }

                return decipheredString;

            }

            private string ConvertUIntToString(uint value) {
                System.Text.StringBuilder output = new System.Text.StringBuilder();
                output.Append((char)(value & 0xFF));
                output.Append((char)((value >> 8) & 0xFF));
                output.Append((char)((value >> 16) & 0xFF));
                output.Append((char)((value >> 24) & 0xFF));
                return output.ToString();
            }

            private uint ConvertStringToUInt(string input) {
                uint output = 0;
                output = ((uint)input[0]);
                output += ((uint)input[1] << 8);
                output += ((uint)input[2] << 16);
                output += ((uint)input[3] << 24);
                return output;
            }
            #endregion Tiny Encryption Algorithm (TEA) Methods
        }
    }
}
