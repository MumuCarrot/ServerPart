/**
 * Ivan Kovalenko
 * 10.03.2024
 */

// NuGet collection
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;




// System collection
using System.Net;
using System.Net.Sockets;
using System.Text;

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
    class Program
    {
        /// <summary>
        /// Online users list
        /// </summary>
        private static readonly List<ClientHandler> onlineUsers = [];

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
            private MySqlCommand command;
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
            Avatar? ava = null;

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

                    // Searching for key word
                    int keyWordIndex = request.IndexOf(' ');
                    if (keyWordIndex == -1) throw new Exception("Key word was not found.");

                    // Getting key word
                    string keyWord = request[..keyWordIndex];
                    switch (keyWord)
                    {
                        case "GET":
                            this.GetRequest(request[(keyWordIndex + 1)..]);
                            break; // GET
                        case "POST":
                            this.PostRequest(request[(keyWordIndex + 1)..]);
                            break; // POST
                        case "PATCH":
                            this.PatchRequest(request[(keyWordIndex + 1)..]);
                            break; // PATCH
                        default: throw new Exception("Method wasn't found. =(");
                    }
                }
            }

            /// <summary>
            /// Get request
            /// </summary>
            /// <param name="request">
            /// Method with request
            /// </param>
            /// <exception cref="Exception">
            /// Method not found
            /// </exception>
            public void GetRequest(string request)
            {
                int methodIndex = request.IndexOf(' ');
                if (methodIndex == -1) throw new Exception("Get method was not found.");

                string methodWord = request[..methodIndex];
                switch (methodWord)
                {
                    case "--USER_CHECK":
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
                        break; // --USER_CHECK
                    case "--ACMSG":
                        int countStart = request.IndexOf("count{") + "count{".Length;
                        int countEnd = request.IndexOf('}', countStart);

                        // Извлекаем подстроку для пароля
                        string count = request[countStart..countEnd];

                        command = new($"SELECT * FROM all_chat ORDER BY msg_id DESC LIMIT " + count + ";", connection);
                        break; // --ACMSG
                }

                if (command is not null)
                {
                    using var reader = command.ExecuteReader();

                    if (reader is not null)
                    {
                        string json = string.Empty;
                        switch (methodWord)
                        {
                            case "--USER_CHECK":
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
                                    json = JsonConvert.SerializeObject(user);
                                    SendMessage($"GET --USER_CHECK json{{{json}}} ANSWER{{status{{true}}}};");
                                }
                                // User not found
                                else if (count < 1)
                                {
                                    SendMessage(request + " ANSWER{status{false}};");
                                }
                                else if (count > 1) throw new Exception("Here was found more then one user by this login or password...");
                                else throw new Exception("Unexpected error!");
                                break; // --USER_CHECK
                            case "--ACMSG":
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

                                json = JsonConvert.SerializeObject(new { messages });

                                SendMessage($"GET --ACMSG json{{{json}}};");
                                break; // --ACMSG
                        }
                        reader.Close();
                    }
                }
                else throw new Exception("Command is null.");

            }

            /// <summary>
            /// Post request
            /// </summary>
            /// <param name="request">
            /// Method with request
            /// </param>
            /// <exception cref="Exception">
            /// Method not found
            /// </exception>
            public void PostRequest(string request)
            {
                int methodIndex = request.IndexOf(' ');
                if (methodIndex == -1) throw new Exception("Post method was not found.");

                string methodWord = request[..methodIndex];
                switch (methodWord)
                {
                    case "--USER":
                        // Находим позиции начала и конца логина
                        int loginStartUser = request.IndexOf("login{") + "login{".Length;
                        int loginEndUser = request.IndexOf('}', loginStartUser);

                        // Извлекаем подстроку для логина
                        string loginUser = request[loginStartUser..loginEndUser];

                        // Находим позиции начала и конца пароля
                        int passwordStartUser = request.IndexOf("password{") + "password{".Length;
                        int passwordEndUser = request.IndexOf('}', passwordStartUser);

                        // Извлекаем подстроку для пароля
                        string passwordUser = request[passwordStartUser..passwordEndUser];

                        command = new($"INSERT INTO users VALUES (\'{loginUser}\', \'{loginUser}\', \'{passwordUser}\');", connection);
                        break; // --USER
                    case "--MSG":
                        int loginStartMsg = request.IndexOf("login{") + "login{".Length;
                        int loginEndMsg = request.IndexOf('}', loginStartMsg);

                        string loginMsg = request[loginStartMsg..loginEndMsg];

                        int contentStartMsg = request.IndexOf("content{") + "content{".Length;
                        int contentEndMsg = request.IndexOf('}', contentStartMsg);

                        string contentMsg = request[contentStartMsg..contentEndMsg];

                        int msg_timeStartMsg = request.IndexOf("msg_time{") + "msg_time{".Length;
                        int msg_timeEndMsg = request.IndexOf('}', msg_timeStartMsg);

                        string msg_timeMsg = request[msg_timeStartMsg..msg_timeEndMsg];

                        int typeStartMsg = request.IndexOf("type{") + "type{".Length;
                        int typeEndMsg = request.IndexOf('}', typeStartMsg);

                        string typeMsg = request[typeStartMsg..typeEndMsg];

                        command = new($"INSERT INTO all_chat(user_login, content, msg_time, msg_type) VALUES (\'{loginMsg}\', \'{contentMsg}\', \'{msg_timeMsg}\', '{typeMsg}');", connection);

                        Message message = new()
                        {
                            MessageDateTime = msg_timeMsg,
                            Login = loginMsg,
                            Content = contentMsg,
                            MessageType = typeMsg
                        };
                        List<Message> messages = [message];
                        string json = JsonConvert.SerializeObject(new { messages });
                        this.SendGlobalMessage($"POST --MSG json{{{json}}}");
                        break; // --MSG
                }

                if (command is not null) 
                { 
                    using var reader = command.ExecuteReader();
                }
                else throw new Exception("Command is null.");

            }

            List<byte[]> byteBush = [];
            /// <summary>
            /// Patch request
            /// </summary>
            /// <param name="request">
            /// Method with request
            /// </param>
            /// <exception cref="Exception">
            /// Method not found
            /// </exception>
            public void PatchRequest(string request)
            {
                int methodIndex = request.IndexOf(' ');
                if (methodIndex == -1) throw new Exception("Patch method was not found.");

                string methodWord = request[..methodIndex];

                string json = string.Empty;
                switch (methodWord)
                {
                    case "--UPD_USER":
                        // Searching for JSON start point
                        int startIndexUpdUser = request.IndexOf("user{") + "user{".Length;
                        if (startIndexUpdUser == -1) throw new Exception("JSON start point wasn't found.");
                        int endIndexUpdUser = request.IndexOf('}') + 1;
                        if (endIndexUpdUser == -1) throw new Exception("JSON end point wasn't found.");

                        // Getting JSON into string
                        json = request[startIndexUpdUser..endIndexUpdUser];

                        User? user = JsonConvert.DeserializeObject<User>(json);

                        if (user is not null)
                        {
                            command = new($"UPDATE users SET username = \"{user.UserName}\", user_login = \"{user.Login}\", about_me = \"{user.AboutMe}\" WHERE user_login = '{user.Login}';", connection);
                        }
                        break; // --UPD_USER
                    case "--UPD_AVATAR":
                        if (request.Contains("avatar"))
                        {
                            byteBush = [];

                            // Searching for JSON start point
                            int startIndexUpdAvatar = request.IndexOf("avatar{") + "avatar{".Length;
                            if (startIndexUpdAvatar == -1) throw new Exception("JSON start point wasn't found.");
                            int endIndexUpdAvatar = request.IndexOf('}') + 1;
                            if (endIndexUpdAvatar == -1) throw new Exception("JSON end point wasn't found.");

                            // Getting JSON into string
                            json = request[startIndexUpdAvatar..endIndexUpdAvatar];

                            ava = JsonConvert.DeserializeObject<Avatar>(json);

                            SendMessage($"PATCH --UPD_AVATAR part{{status{{ready}}}}");

                            Console.WriteLine("open");
                        }
                        else if (request.Contains("part"))
                        {
                            // Searching for JSON start point
                            int startIndexUpdAvatar = request.IndexOf("part{") + "part{".Length;
                            if (startIndexUpdAvatar == -1) throw new Exception("JSON start point wasn't found.");
                            int endIndexUpdAvatar = request.IndexOf('}') + 1;
                            if (endIndexUpdAvatar == -1) throw new Exception("JSON end point wasn't found.");

                            json = request[startIndexUpdAvatar..(endIndexUpdAvatar - 1)];

                            byteBush.Add(JsonConvert.DeserializeObject<byte[]>(json) ?? [0]);

                            SendMessage($"PATCH --UPD_AVATAR part{{status{{ready}}}}");

                            Console.WriteLine("part");
                        }
                        else if (request.Contains("close"))
                        {
                            if (ava is not null)
                            {
                                Console.WriteLine("close");

                                byte[] combinedBytes = byteBush.SelectMany(bytes => bytes).ToArray();

#pragma warning disable CA1416

                                string imgName = $"pp{{user{{{ava.Login}}}time{{{DateTime.Now:HHmmddMMyy}}}}}.jpg";
                                string folderPath = "C:\\ServerPictures\\pp\\";

                                if (Directory.Exists(folderPath))
                                {

                                    string[] files = Directory.GetFiles(folderPath);

                                    foreach (string file in files)
                                    {
                                        string fileName = Path.GetFileName(file);

                                        if (fileName.Contains(ava.Login))
                                        {
                                            File.Delete(file);
                                            Console.WriteLine($"Файл {fileName} удален.");
                                        }
                                    }
                                }

                                using (var image = Image.FromStream(new MemoryStream(combinedBytes)))
                                {
                                    using var memoryStream = new MemoryStream();
                                    image.Save(folderPath + imgName, ImageFormat.Jpeg);
                                }

#pragma warning restore CA1416

                                command = new("UPDATE users SET avatar = \"" + imgName + $"\" WHERE user_login = \"{ava.Login}\";", connection);

                                if (command is not null)
                                {
                                    using var reader = command.ExecuteReader();
                                }
                                else throw new Exception("Command is null.");

                                SendMessage($"PATCH --UPD_AVATAR name{{{imgName}}}, status{{close}}");
                            }
                        }

                        break; // --UPD_AVATAR
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