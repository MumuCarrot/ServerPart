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

        private partial class Client
        {
            private static T? JsonExtractor<T>(string json, string keyWord, int correction = 0)
            {
                // Searching for JSON start point
                int start = json.IndexOf($"{keyWord}{{") + $"{keyWord}{{".Length;
                if (start == -1) throw new Exception("JSON start point wasn't found.");
                int end = json.IndexOf('}', start);
                if (end == -1) throw new Exception("JSON end point wasn't found.");

                return JsonConvert.DeserializeObject<T>(json[start..(end - correction)]);
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
