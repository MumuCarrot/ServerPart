using Newtonsoft.Json;
using System.Text;

namespace Connect.server
{
    public partial class Server
    {
        /// <summary>
        /// Sending message to all online users
        /// </summary>
        /// <param name="sender">
        /// Ignore this user
        /// </param>
        /// <param name="message">
        /// Message
        /// </param>
        private void GlobalMessage(Client sender, string message)
        {
            var newOnlineUsers = onlineUsers;
            foreach (var client in newOnlineUsers)
            {
                try
                {
                    if (client != sender)
                        client.SendMessage(message);
                }
                catch
                {
                    onlineUsers.Remove(client);
                }
            }
        }

        /// <summary>
        /// Part of client
        /// </summary>
        private partial class Client
        {
            /// <summary>
            /// Json extractor
            /// </summary>
            /// <typeparam name="T">
            /// The class to be obtained
            /// </typeparam>
            /// <param name="json">
            /// String with json
            /// </param>
            /// <param name="keyWord">
            /// Keyword followed by json
            /// </param>
            /// <param name="left">
            /// Left shift
            /// </param>
            /// <param name="right">
            /// Right shift
            /// </param>
            /// <returns>
            /// New element of T
            /// </returns>
            private static T? JsonExtractor<T>(string json, string keyWord, int left = 0, int right = 0)
            {
                string str = string.Empty;
                try
                {
                    // Searching for JSON start point
                    int start = json.IndexOf($"{keyWord}{{") + $"{keyWord}{{".Length;
                    if (start == -1) throw new Exception("JSON start point wasn't found.");
                    int endpointStart = json.LastIndexOf("},") + "},".Length;
                    if (endpointStart == -1) endpointStart = start;
                    int end = json.IndexOf('}', endpointStart);
                    if (end == -1) throw new Exception("JSON end point wasn't found.");

                    str = json[(start + left)..(end + right)];

                    return JsonConvert.DeserializeObject<T>(str);
                }
                catch
                {
                    Console.WriteLine($"json:\n{json}\n\n");
                    Console.WriteLine($"str:\n{str}\n");
                    return default;
                }
            }

            /// <summary>
            /// Recive a message and send it to the client
            /// </summary>
            /// <param name="message">
            /// Message
            /// </param>
            /// <exception cref="Exception">
            /// Stream is null.
            /// </exception>
            public void SendMessage(string message)
            {
                byte[] responseMessageBytes = Encoding.UTF8.GetBytes(message + Environment.NewLine);
                if (stream is not null)
                {
                    stream.Write(responseMessageBytes, 0, responseMessageBytes.Length);
                    stream.Flush();
                }
                else throw new Exception("Stream is null.");
            }

            /// <summary>
            /// Sending message to all online users
            /// </summary>
            /// <param name="message">
            /// Message
            /// </param>
            public void SendGlobalMessage(string message)
            {
                server.GlobalMessage(this, message);
            }
        }
    }
}
