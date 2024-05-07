using Connect.message;
using Connect.user;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Connect.server
{
    public partial class Server
    {
        private partial class Client
        {
            /// <summary>
            /// Get request
            /// </summary>
            /// <param name="request">
            /// Method with request
            /// </param>
            /// <exception cref="Exception">
            /// Method not found
            /// </exception>
            private void GetRequest(string request)
            {
                int methodIndex = request.IndexOf(' ');
                if (methodIndex == -1) throw new Exception("Get method was not found.");

                string methodWord = request[..methodIndex];
                switch (methodWord)
                {
                    case "--LOG-IN":
                        this.GetUserCheck(request);
                        break; // --LOG-IN
                    case "--CHAT-HISTORY":
                        this.GetUpdateChat(request);
                        break; // --CHAT-HISTORY
                    case "--USER-LIST":
                        this.GetUsersByLogin(request);
                        break; // --USER-LIST
                    case "--CHAT-LIST":
                        this.GetUpdateChatList(request);
                        break; // --CHAT-LIST
                    case "--CHAT-PICTURE":
                        this.GetUpdateChatPicture(request);
                        break; // --CHAT-PICTURE
                }
            }

            /// <summary>
            /// Search for user by Login and Password
            /// </summary>
            /// <param name="request"></param>
            /// <exception cref="Exception"></exception>
            private void GetUserCheck(string request)
            {
                User? user = JsonExtractor<User>(request, "json", right: 2);

                if (user is not null)
                {
                    command = new($"SELECT username, user_login, user_password, IFNULL(about_me, ''), IFNULL(profile_picture, ''), IFNULL(profile_background, '') " +
                                  $"FROM users " +
                                  $"WHERE(user_login = \'{user.Login}\' " +
                                  $"AND user_password = \'{user.Password}\');", connection);

                    using var reader = command.ExecuteReader();

                    int count = 0;
                    while (reader.Read())
                    {
                        count++;
                        try
                        {
                            user = new()
                            {
                                UserName = reader.GetString(0),
                                Login = reader.GetString(1),
                                Password = reader.GetString(2),
                                AboutMe = reader.GetString(3),
                                UserProfilePicture = new()
                                {
                                    PictureName = reader.GetString(4),
                                    PPColor = reader.GetString(5),
                                }
                            };
                        }
                        catch
                        {
                            count = -1;
                            break;
                        }
                    }

                    // User found
                    if (count == 1)
                    {
                        string json = JsonConvert.SerializeObject(user);
                        SendMessage($"GET --LOG-IN json{{{json}}}");
                    }
                    // User not found
                    else if (count < 1)
                    {
                        SendMessage("GET --LOG-IN FALSE");
                    }
                    else if (count > 1) throw new Exception("Here was found more then one user by this login or password...");
                    else throw new Exception("Unexpected error!");
                }
            }

            /// <summary>
            /// Search for a chat history of current user
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void GetUpdateChat(string request)
            {
                string[]? information = JsonExtractor<string[]>(request, "json");

                if (information is not null)
                {
                    var filter = Builders<Chat>.Filter.
                        Eq(c => c.Id, ObjectId.Parse(information[0]));

                    var projection = Builders<Chat>.Projection.
                        Slice(c => c.Messages, 0, int.Parse(information[1]));

                    var result = collection.Find(filter).Project(projection);

                    List<Chat> chatList = [];
                    foreach (var i in result.ToList())
                    {
                        chatList.Add(BsonSerializer.Deserialize<Chat>(i));
                    }

                    string json = JsonConvert.SerializeObject(chatList);

                    SendMessage($"GET --CHAT-HISTORY json{{{json}}};");
                }
            }

            /// <summary>
            /// Search for user by Login
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void GetUsersByLogin(string request)
            {
                string[]? str = JsonExtractor<string[]>(request, "json");

                if (str is not null)
                {
                    command = new($"SELECT username, user_login, user_password, IFNULL(about_me, ''), IFNULL(profile_picture, ''), IFNULL(profile_background, '') " +
                                  $"FROM users " +
                                  $"WHERE user_login " +
                                  $"LIKE '%{str[1]}%' " +
                                  $"AND user_login != '{str[0]}' " +
                                  $"LIMIT 6;", connection);

                    using var reader = command.ExecuteReader();

                    UserPackege users = new();
                    while (reader.Read())
                    {
                        users.users.Add(new User
                        {
                            UserName = reader.GetString(0),
                            Login = reader.GetString(1),
                            Password = reader.GetString(2),
                            AboutMe = reader.GetString(3),
                            UserProfilePicture = new()
                            {
                                PictureName = reader.GetString(4),
                                PPColor = reader.GetString(5),
                            }
                        });
                    }

                    string json = JsonConvert.SerializeObject(users);

                    SendMessage($"GET --USER-LIST json{{{json}}}");
                }
            }

            /// <summary>
            /// Search for a list of chats of current user
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void GetUpdateChatList(string request)
            {
                string? login = JsonExtractor<string>(request, "json");

                if (login is not null)
                {
                    var aggregate = collection.Aggregate()
                        .Match(c => c.Chatusers.Contains(login))
                        .Project(c => new
                        {
                            c.Id,
                            c.Chatusers,
                            LastMessage = c.Messages.OrderByDescending(m => m.Time).FirstOrDefault()
                        });

                    var result = aggregate.ToList();

                    List<Chat> chatList = [];
                    foreach (var chat in result)
                    {
                        chatList.Add(new Chat
                        {
                            Id = chat.Id,
                            Chatusers = chat.Chatusers,
                            Messages =
                            [
                                new Message
                                {
                                    Username = chat.LastMessage?.Username,
                                    Content = chat.LastMessage?.Content,
                                    Time = chat.LastMessage?.Time
                                }
                            ]
                        });
                    }

                    string json = JsonConvert.SerializeObject(chatList);

                    SendMessage($"GET --CHAT-LIST json{{{json}}};");
                }
            }

            /// <summary>
            /// Search for chat image(s)
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void GetUpdateChatPicture(string request)
            {
                (string?, string?[])? deq = JsonExtractor<(string?, string?[])>(request, "json", right: 1);

                if (deq is not null && deq is not (null, null) && deq.Value.Item1?.Length > 0 && deq.Value.Item2.Length > 0)
                {
                    // Searching for chats with user
                    var aggregate = collection.Aggregate()
                        .Match(c => c.Chatusers.Contains(deq.Value.Item1 ?? "null"))
                        .Project(c => new
                        {
                            c.Id,
                            c.Chatusers
                        });

                    var result = aggregate.ToList();

                    List<Chat> chatList = [];
                    foreach (var chat in result)
                    {
                        chatList.Add(new Chat
                        {
                            Id = chat.Id,
                            Chatusers = chat.Chatusers
                        });
                    }

                    // Filtring chatusers
                    Dictionary<string, string> userIdPair = [];
                    foreach (var chat in chatList)
                    {
                        if (deq.Value.Item2.Contains(chat.Id.ToString()))
                        {
                            foreach (var user in chat.Chatusers)
                            {
                                if (user != deq.Value.Item1)
                                {
                                    userIdPair.Add(user, chat.Id.ToString());
                                }
                            }
                        }
                    }

                    // Searching for users profile picture
                    command = new($"SELECT user_login, IFNULL(profile_picture, ''), IFNULL(profile_background, '') " +
                                  $"FROM users " +
                                  $"WHERE user_login " +
                                  $"IN ({string.Join(", ", userIdPair.Keys.Select(value => $"'{value}'"))});", connection);

                    using var reader = command.ExecuteReader();

                    // Reading answer
                    Dictionary<string, (string, string)> answer = [];
                    while (reader.Read())
                    {
                        if (userIdPair.ContainsKey(reader.GetString(0)))
                        {
                            answer.Add(userIdPair[reader.GetString(0)], (reader.GetString(1), reader.GetString(2)));
                        }
                    }

                    string json = JsonConvert.SerializeObject(answer);

                    SendMessage($"GET --CHAT-PICTURE json{{{json}}};");
                }
            }
        }
    }
}
