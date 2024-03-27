using Connect.message;
using Connect.user;
using Newtonsoft.Json;

namespace Connect.server
{
    public partial class Server
    {
        private partial class Client
        {
            private void GetUserCheck(string request)
            {
                User? user = JsonExtractor<User>(request, "json", 0);

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
                    command = new($"SELECT * FROM all_chat ORDER BY msg_id DESC LIMIT {count} ;", connection);

                    using var reader = command.ExecuteReader();

                    List<Message> messages = [];

                    while (reader.Read())
                    {
                        messages.Add(new Message()
                        {
                            MessageDateTime = reader.GetString(2),  // Date and Time
                            Login = reader.GetString(0),            // Username
                            Content = reader.GetString(1),          // Message
                            MessageType = reader.GetString(3)       // Type of message
                        });
                    }
                    messages.Reverse();

                    string json = JsonConvert.SerializeObject(new { messages });

                    SendMessage($"GET --ACMSG json{{{json}}};");
                }
            }

            private void GetUsersByLogin(string request)
            {
                string? str = JsonExtractor<string>(request, "json", 0);

                if (str is not null)
                {
                    command = new($"SELECT username, user_login, user_password, about_me, avatar FROM users WHERE user_login LIKE '%{str}%';", connection);

                    using var reader = command.ExecuteReader();

                    UserPackege users = new();
                    foreach (var user in reader)
                    {
                        if (user is not null && user is User u)
                            users.users.Add(u);
                    }

                    string json = JsonConvert.SerializeObject(users);

                    SendMessage($"GET --UBYLOG json{{{json}}};");
                }
            }
        }
    }
}
