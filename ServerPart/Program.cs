/**
 * Ivan Kovalenko
 * 05.03.2024
 */

// NuGet collection
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

// System collection
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

/**
 * Server is parted on two parts
 * 
 * FIRST
 * Server Starter 
 * should initcializate threads and produce a connection
 * 
 * SECOND
 * Customer service
 * should listen and answer to the client
 */
namespace Server
{
    /// <summary>
    /// Program entrence point
    /// </summary>
    partial class Program
    {
        /// <summary>
        /// Online users list
        /// </summary>
        protected static readonly List<ClientHandler> onlineUsers = [];

        /// <summary>
        /// Server starter
        /// </summary>
        private partial class Server
        {
            #region Fields
            /// <summary>
            /// mySql data base location
            /// </summary>
            public static string ConnectionString { get; } = "server=localhost;uid=root;pwd=1234;database=mydb;";
            /// <summary>
            /// Server's IP
            /// </summary>
            public static string IP { get; } = "127.0.0.1";
            /// <summary>
            /// Server's port
            /// </summary>
            public static int Port { get; } = 7007;
            /// <summary>
            /// Thread that wait for a client,
            /// will produce more threads for a listen/answer to client
            /// </summary>
            private readonly Thread? listenerThread;
            /// <summary>
            /// TCP listener
            /// </summary>
            private readonly TcpListener? tcpListener;
            /// <summary>
            /// mySql connection
            /// </summary>
            private readonly MySqlConnection connection;
            /// <summary>
            /// mySql command
            /// </summary>
            private readonly MySqlCommand command;
            #endregion

            /// <summary>
            /// Server primary constructor
            /// </summary>
            public Server()
            {
                // TCP listener start
                this.tcpListener = new(IPAddress.Parse(IP), Port);
                this.tcpListener.Start();

                // Search for mySql data base
                this.connection = new(ConnectionString);
                connection.Open();

                // Creation of mySql command
                this.command = connection.CreateCommand();

                // Creation of listener thread
                this.listenerThread = new Thread(new ThreadStart(ListenForClients));
                this.listenerThread.Start();
            }

            /// <summary>
            /// Client listener
            /// </summary>
            private void ListenForClients()
            {
                try
                {
                    if (this.tcpListener is not null)
                    {
                        Console.WriteLine(
                            $"""
                        SYSTEM:
                                Server has started on {IP}:{Port}.
                                Waiting for a connection...
                        """);

                        while (true)
                        {
                            // Wait for a client, thread is freezed until client connected.
                            TcpClient client = this.tcpListener.AcceptTcpClient();

                            // Client connected
                            Console.WriteLine("Client is connected.");

                            // !!! Here is happening all the magic <3 !!!
                            // Creating client hendler class
                            ClientHandler clientH = new(client, connection, command);

                            //if (!onlineUsers.Contains(clientH))
                            onlineUsers.Add(clientH);

                            // creating thread for a client listener
                            Thread clientThread = new(new ThreadStart(clientH.Start));
                            // starting the process
                            clientThread.Start();
                        }
                    }
                    else throw new Exception("TCP listener is null.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            /// <summary>
            /// Sending message to all online users
            /// </summary>
            /// <param name="sender">
            /// Ignore this user
            /// </param>
            /// <param name="message">
            /// Message
            /// </param>
            public static void GlobalMessage(ClientHandler sender, string message)
            {
                var newOnlineUsers = onlineUsers;
                foreach (var cl in newOnlineUsers)
                {
                    try
                    {
                        if (cl != sender)
                            cl.SendMessage(message);
                    }
                    catch
                    {
                        onlineUsers.Remove(cl);
                    }
                }
            }
        }

        /// <summary>
        /// Customer service
        /// </summary>
        /// <param name="client">
        /// Client
        /// </param>
        /// <param name="connection">
        /// mySql connection
        /// </param>
        /// <param name="command">
        /// mySql comment
        /// </param>
        public partial class ClientHandler(TcpClient client, MySqlConnection connection, MySqlCommand command)
        {
            // Creating connection with a client
            private NetworkStream stream = client.GetStream();

            /// <summary>
            /// Starting listening client
            /// </summary>
            /// <exception cref="Exception">
            /// Raise an exception when mySql comman is empty or null
            /// </exception>
            public void Start()
            {
                while (true)
                {
                    // Wait for data
                    while (!stream.DataAvailable) Thread.Sleep(500);
                    // Byte buffer -> Read request -> Encode
                    byte[] bytes = new byte[client.Available];
                    stream.Read(bytes, 0, bytes.Length);
                    string request = Encoding.UTF8.GetString(bytes);

                    // Creating mySql reader
                    MySqlDataReader reader;

                    // GET request
                    #region GET region
                    if (GetRegex().IsMatch(request))
                    {
                        Console.WriteLine($"GET request was recived.\n{request}\n\n");
                        // Проверка на существующего юзера
                        if (request.Contains("--USER_CHECK"))
                        {
                            // Находим позиции начала и конца логина
                            int loginStart = request.IndexOf("login{") + "login{".Length;
                            int loginEnd = request.IndexOf('}', loginStart);

                            // Извлекаем подстроку для логина
                            string login = request[loginStart..loginEnd];

                            // Находим позиции начала и конца пароля
                            int passwordStart = request.IndexOf("password{") + "password{".Length;
                            int passwordEnd = request.IndexOf('}', passwordStart);

                            // Извлекаем подстроку для пароля
                            string password = request[passwordStart..passwordEnd];

                            command = new($"SELECT * FROM users WHERE(user_login = \'{login}\' AND user_password = \'{password}\');", connection);
                        }
                        else if (request.Contains("--ACMSG"))
                        {
                            int countStart = request.IndexOf("count{") + "count{".Length;
                            int countEnd = request.IndexOf('}', countStart);

                            // Извлекаем подстроку для пароля
                            string count = request[countStart..countEnd];

                            command = new($"SELECT * FROM all_chat ORDER BY msg_id DESC LIMIT " + count + ";", connection);
                        }

                        if (command is not null)
                            reader = command.ExecuteReader();
                        else throw new Exception("Command is null.");

                        if (request.Contains("--USER_CHECK"))
                        {
                            User user = new();
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
                        else if (request.Contains("--ACMSG"))
                        {
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

                        reader.Close();
                    }
                    #endregion

                    // POST request
                    #region POST region
                    else if (PostRegex().IsMatch(request))
                    {
                        Console.WriteLine("POST request was recived.");
                        if (request.Contains("--USER"))
                        {
                            // Находим позиции начала и конца логина
                            int loginStart = request.IndexOf("login{") + "login{".Length;
                            int loginEnd = request.IndexOf('}', loginStart);

                            // Извлекаем подстроку для логина
                            string login = request[loginStart..loginEnd];

                            // Находим позиции начала и конца пароля
                            int passwordStart = request.IndexOf("password{") + "password{".Length;
                            int passwordEnd = request.IndexOf('}', passwordStart);

                            // Извлекаем подстроку для пароля
                            string password = request[passwordStart..passwordEnd];

                            command = new($"INSERT INTO users VALUES (\'{login}\', \'{login}\', \'{password}\');", connection);

                            if (command is not null)
                                reader = command.ExecuteReader();
                            else throw new Exception("Command is null.");

                            reader.Close();
                        }
                        else if (request.Contains("--MSG"))
                        {
                            int loginStart = request.IndexOf("login{") + "login{".Length;
                            int loginEnd = request.IndexOf('}', loginStart);

                            string login = request[loginStart..loginEnd];

                            int contentStart = request.IndexOf("content{") + "content{".Length;
                            int contentEnd = request.IndexOf('}', contentStart);

                            string content = request[contentStart..contentEnd];

                            int msg_timeStart = request.IndexOf("msg_time{") + "msg_time{".Length;
                            int msg_timeEnd = request.IndexOf('}', msg_timeStart);

                            string msg_time = request[msg_timeStart..msg_timeEnd];

                            int typeStart = request.IndexOf("type{") + "type{".Length;
                            int typeEnd = request.IndexOf('}', typeStart);

                            string type = request[typeStart..typeEnd];

                            command = new($"INSERT INTO all_chat(user_login, content, msg_time, msg_type) VALUES (\'{login}\', \'{content}\', \'{msg_time}\', '{type}');", connection);

                            Message message = new()
                            {
                                MessageDateTime = msg_time,
                                Login = login,
                                Content = content,
                                MessageType = type
                            };
                            List<Message> messages = [message];
                            string json = JsonConvert.SerializeObject(new { messages });
                            SendGlobalMessage($"POST --MSG json{{{json}}}");

                            if (command is not null)
                                reader = command.ExecuteReader();
                            else throw new Exception("Command is null.");

                            reader.Close();
                        }
                    }
                    #endregion

                    else if (request.Contains("PATCH")) 
                    {
                        if (request.Contains("--UPD_USER")) 
                        {
                            // Searching for JSON start point
                            int startIndex = request.IndexOf("user{") + "user{".Length;
                            if (startIndex == -1) throw new Exception("JSON start point wasn't found.");
                            int endIndex = request.IndexOf('}') + 1;
                            if (endIndex == -1) throw new Exception("JSON end point wasn't found.");

                            // Getting JSON into string
                            string json = request[startIndex..endIndex];

                            User? user = JsonConvert.DeserializeObject<User>(json);

                            if (user is not null) 
                            { 
                                command = new($"UPDATE users SET username = \"{user.UserName}\", user_login = \"{user.Login}\", about_me = \"{user.AboutMe}\" WHERE user_login = '{user.Login}';", connection);
                            }

                            if (command is not null)
                                command.ExecuteReader();
                            else throw new Exception("Command is null.");
                        }
                    }

                    // Default region
                    #region default region
                    else
                    {
                        Console.WriteLine("Something gone wrong.");
                    }
                    #endregion
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
                Server.GlobalMessage(this, message);
            }

            #region Regex region
            [GeneratedRegex("^POST.+$")]
            private static partial Regex PostRegex();
            [GeneratedRegex("^GET.+$")]
            private static partial Regex GetRegex();
            #endregion
        }

        /// <summary>
        /// Message class 
        /// </summary>
        public class Message
        {
            [JsonProperty("datetime")]
            public string MessageDateTime { get; set; } = string.Empty;
            [JsonProperty("login")]
            public string Login { get; set; } = string.Empty;
            [JsonProperty("content")]
            public string Content { get; set; } = string.Empty;
            [JsonProperty("type")]
            public string MessageType { get; set; } = string.Empty;
        }

        public class User 
        {
            [JsonProperty("username")]
            public string UserName { get; set; } = string.Empty;
            [JsonProperty("login")]
            public string Login { get; set; } = string.Empty;
            [JsonProperty("password")]
            public string Password { get; set; } = string.Empty;
            [JsonProperty("aboutme")]
            public string AboutMe { get; set; } = string.Empty;
        }

        /// <summary>
        /// Entrence
        /// </summary>
        static void Main()
        {
            _ = new Server();
        }
    }
}