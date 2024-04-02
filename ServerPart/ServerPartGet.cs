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
                    case "--ACMSG":
                        this.GetUpdateChat(request);
                        break; // --ACMSG
                    case "--UBYLOG":
                        this.GetUsersByLogin(request);
                        break; // --UBYLOG
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
                int? count = JsonExtractor<int>(request, "json", 0);

                if (count is not null)
                {
                    /*command = new($"SELECT * FROM all_chat ORDER BY msg_id DESC LIMIT {count} ;", connection);

                    using var reader = command.ExecuteReader();

                    List<Message> messages = [];

                    while (reader.Read())
                    {
                        string content = reader.GetString(1);
                        if (content.Contains("&#cO"))
                        {
                            content = content.Replace("&#cO", "\'");
                        }

                        messages.Add(new Message()
                        {
                            MessageDateTime = reader.GetString(2),  // Date and Time
                            Login = reader.GetString(0),            // Username
                            Content = content,                      // Message
                            MessageType = reader.GetString(3)       // Type of message
                        });
                    }
                    messages.Reverse();*/

                    List<Message> messages = [];

                    var filter = Builders<Chat>.Filter.Eq(c => c.Id, ObjectId.Parse("660afbf1b76620cd7544eefe"));

                    var result = collection.Find(filter);

                    foreach (var i in result.ToList<Chat>())
                    {
                        if (i.Messages is not null)
                        {
                            foreach (var j in i.Messages)
                            {
                                messages.Add(j);
                            }
                        }
                    }

                    string json = JsonConvert.SerializeObject(messages);

                    SendMessage($"GET --ACMSG json{{{json}}};");
                }
            }

            private void GetUsersByLogin(string request)
            {
                string[]? str = JsonExtractor<string[]>(request, "json", 0);

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

                    SendMessage($"GET --UBYLOG json{{{json}}};");
                }
            }
        }
    }
}
