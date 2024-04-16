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
                    case "--USER_CHECK":
                        this.GetUserCheck(request);
                        break; // --USER_CHECK
                    case "--CHAT-HISTORY":
                        this.GetUpdateChat(request);
                        break; // --CHAT-HISTORY
                    case "--USER-LIST":
                        this.GetUsersByLogin(request);
                        break; // --USER-LIST
                    case "--CHAT-LIST":
                        this.GetUpdateChatList(request);
                        break; // --CHAT-LIST
                }
            }

            private void GetUserCheck(string request)
            {
                User? user = JsonExtractor<User>(request, "json", 0, 1);

                if (user is not null)
                {
                    command = new($"SELECT * FROM users WHERE(user_login = \'{user.Login}\' AND user_password = \'{user.Password}\');", connection);

                    using var reader = command.ExecuteReader();

                    int count = 0;
                    while (reader.Read())
                    {
                        count++;
                        try
                        {
                            string aboutMe;
                            try
                            {
                                aboutMe = reader.GetString(3);
                            }
                            catch
                            {
                                aboutMe = string.Empty;
                            }
                            user = new()
                            {
                                UserName = reader.GetString(0),
                                Login = reader.GetString(1),
                                Password = reader.GetString(2),
                                AboutMe = aboutMe
                            };
                        }
                        catch
                        {
                            count = 99;
                            break;
                        }
                    }

                    // User found
                    if (count == 1)
                    {
                        string json = JsonConvert.SerializeObject(user);
                        SendMessage($"GET --USER_CHECK json{{{json}}} ANSWER{{status{{true}}}};");
                    }
                    // User not found
                    else if (count < 1)
                    {
                        SendMessage(request + " ANSWER{status{false}};");
                    }
                    else if (count > 1) throw new Exception("Here was found more then one user by this login or password...");
                    else throw new Exception("Unexpected error!");
                }
            }

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

            private void GetUsersByLogin(string request)
            {
                string[]? str = JsonExtractor<string[]>(request, "json");

                if (str is not null)
                {
                    command = new($"SELECT username, user_login, user_password, about_me, avatar " +
                                  $"FROM users " +
                                  $"WHERE user_login " +
                                  $"LIKE '%{str[1]}%' " +
                                  $"AND user_login != '{str[0]}' " +
                                  $"LIMIT 6;", connection);

                    using var reader = command.ExecuteReader();

                    UserPackege users = new();
                    while (reader.Read())
                    {
                        string username;
                        string login;
                        string password;
                        string aboutme;
                        string pppath;

                        try { username = reader.GetString(0); }
                        catch { username = string.Empty; }

                        try { login = reader.GetString(1); }
                        catch { login = string.Empty; }

                        try { password = reader.GetString(2); }
                        catch { password = string.Empty; }

                        try { aboutme = reader.GetString(3); }
                        catch { aboutme = string.Empty; }

                        try { pppath = reader.GetString(4); }
                        catch { pppath = string.Empty; }

                        users.users.Add(new User
                        {
                            UserName = username,
                            Login = login,
                            Password = password,
                            AboutMe = aboutme,
                            ProfilePicturePath = pppath
                        });
                    }

                    string json = JsonConvert.SerializeObject(users);

                    SendMessage($"GET --USER-LIST json{{{json}}};");
                }
            }

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
        }
    }
}
