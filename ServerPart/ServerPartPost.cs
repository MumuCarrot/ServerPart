using Connect.message;
using Connect.user;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Connect.server
{
    public partial class Server
    {
        private partial class Client
        {
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
