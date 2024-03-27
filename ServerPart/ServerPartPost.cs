using Connect.message;
using Connect.user;
using Newtonsoft.Json;

namespace Connect.server
{
    public partial class Server
    {
        private partial class Client
        {
            /// <summary>
            /// Post request
            /// </summary>
            /// <param name="request">
            /// Method with request
            /// </param>
            /// <exception cref="Exception">
            /// Method not found
            /// </exception>
            private void PostRequest(string request)
            {
                int methodIndex = request.IndexOf(' ');
                if (methodIndex == -1) throw new Exception("Post method was not found.");

                string methodWord = request[..methodIndex];
                switch (methodWord)
                {
                    case "--USER":
                        this.PostUser(request);
                        break; // --USER
                    case "--MSG":
                        this.PostMessage(request);
                        break; // --MSG
                }
            }

            private void PostUser(string request)
            {
                User? user = JsonExtractor<User>(request, "json", 0);

                if (user is not null)
                {
                    command = new($"INSERT INTO users VALUES (\'{user.Login}\', \'{user.Login}\', \'{user.Password}\');", connection);

                    using var reader = command.ExecuteReader();
                }
            }

            private void PostMessage(string request)
            {
                Message? message = JsonExtractor<Message>(request, "json", 0);

                if (message is not null)
                {
                    command = new($"INSERT INTO all_chat(user_login, content, msg_time, msg_type) VALUES (\'{message.Login}\', \'{message.Content}\', \'{message.MessageDateTime}\', '{message.MessageType}');", connection);

                    using var reader = command.ExecuteReader();

                    List<Message> messages = [message];
                    string json = JsonConvert.SerializeObject(new { messages });

                    this.SendGlobalMessage($"POST --MSG json{{{json}}}");
                }
            }
        }
    }
}
