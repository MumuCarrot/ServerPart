using Connect.message;
using Connect.user;
using MongoDB.Bson;
using MongoDB.Driver;
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
                    case "--CHAT":
                        this.PostChat(request);
                        break; // --CHAT
                }
            }

            /// <summary>
            /// Create new user
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void PostUser(string request)
            {
                User? user = JsonExtractor<User>(request, "json", 0);

                if (user is not null)
                {
                    command = new($"INSERT INTO users " +
                                  $"VALUES (\'{user.Login}\', \'{user.Login}\', \'{user.Password}\');", connection);

                    using var reader = command.ExecuteReader();
                }
            }

            /// <summary>
            /// Create new message
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void PostMessage(string request)
            {
                KeyValuePair<string, Message> kvp = JsonExtractor<KeyValuePair<string, Message>>(request, "json", right: 2);

                string? chatId = kvp.Key;
                Message? message = kvp.Value;

                if (message is not null && chatId is not null)
                {
                    string content = message.Content?.Text ?? "";
                    if (content.Contains('\''))
                    {
                        content = message.Content?.Text?.Replace("\'", "&#cO") ?? "null";
                    }

                    var filter = Builders<Chat>.Filter.Eq(c => c.Id, ObjectId.Parse(chatId));

                    var update = Builders<Chat>.Update.Push(c => c.Messages, message);

                    collection?.UpdateOne(filter, update);

                    string json = JsonConvert.SerializeObject(message);

                    this.SendGlobalMessage($"POST --MSG json{{{json}}}");
                }
            }

            /// <summary>
            /// Create a new chat
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void PostChat(string request)
            {
                var users = JsonExtractor<List<string>>(request, "json");

                if (users is not null)
                {
                    string json = string.Empty;

                    try
                    {
                        Chat newChat = new()
                        {
                            Chatusers = users.ToArray(),
                        };

                        collection?.InsertOne(newChat);

                        SendMessage("POST --CHAT TRUE");
                    }
                    catch
                    {
                        SendMessage("POST --CHAT FLASE");
                    }
                }
            }
        }
    }
}