﻿// Connect collection
using Connect.message;
using MongoDB.Driver;
// NuGet collection
using MySql.Data.MySqlClient;
// System collection
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Connect.server
{
    /// <summary>
    /// Server starter
    /// </summary>
    public partial class Server
    {
        /// <summary>
        /// Online users list
        /// </summary>
        private readonly List<Client> onlineUsers = [];

        /// <summary>
        /// Thread that wait for a client,
        /// will produce more threads for a listen/answer to client
        /// </summary>
        private Thread? listenerThread;

        /// <summary>
        /// TCP listener
        /// </summary>
        private readonly TcpListener? tcpListener;

        /// <summary>
        /// mySql connection
        /// </summary>
        private MySqlConnection? connection;

        /// <summary>
        /// mySql command
        /// </summary>
        private MySqlCommand? command;

        /// <summary>
        /// Name of Mongo database
        /// </summary>
        private const string MongoDatabaseName = "ChatList";

        /// <summary>
        /// Name of Mongo collection
        /// </summary>
        private const string MongoCollectionName = "chats";

        /// <summary>
        /// Mongo client
        /// </summary>
        private MongoClient? mongoClient;

        /// <summary>
        /// Mongo collection
        /// </summary>
        private static IMongoCollection<Chat>? collection;

        /// <summary>
        /// Is TCP server started
        /// </summary>
        public bool Connected = false;

        /// <summary>
        /// Is MySql database connected
        /// </summary>
        public bool IsMySqlConnected = false;

        /// <summary>
        /// Is MySql command created
        /// </summary>
        public bool IsMySqlCommandCreated = false;

        /// <summary>
        /// Is MongoDB connected
        /// </summary>
        public bool IsMongoDBConnected = false;

        /// <summary>
        /// IS MongoDB collection was found
        /// </summary>
        public bool IsMongoDBCollectionFound = false;

        /// <summary>
        /// Is server running
        /// </summary>
        public bool IsServerRunning = false;

        /// <summary>
        /// Server primary constructor
        /// </summary>
        /// <param name="ip">
        /// Server ip
        /// </param>
        /// <param name="port">
        /// Server port
        /// </param>
        public Server(string ip, int port)
        {
            try
            {
                // TCP listener start
                this.tcpListener = new(IPAddress.Parse(ip), port);
                this.tcpListener.Start();

                Connected = true;

                Console.WriteLine($"Server has started on {ip}:{port}.");
            }
            catch
            {
                Console.WriteLine("TCP connection failed...");
            }
        }

        /// <summary>
        /// Set's database by path
        /// </summary>
        /// <param name="path">
        /// Database path
        /// </param>
        public void SetDBPath(string mySqlPath, string MongoDBPath)
        {
            try
            {
                // Search for mySql user database
                this.connection = new(mySqlPath);
                connection.Open();
                IsMySqlConnected = true;

                // Creation of mySql user command
                this.command = connection.CreateCommand();
                IsMySqlCommandCreated = true;

                // Search for MongoDB data base
                mongoClient = new(MongoDBPath);
                IsMongoDBConnected = true;

                // Connecting to the MongoDB messageList collection
                collection = mongoClient.GetDatabase(MongoDatabaseName).GetCollection<Chat>(MongoCollectionName);
                IsMongoDBCollectionFound = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Start server
        /// </summary>
        public void Start()
        {
            if (!IsServerRunning)
            {
                // Creation of listener thread
                this.listenerThread = new Thread(new ThreadStart(ListenForClients));
                this.listenerThread.Start();
                IsServerRunning = true;
            }
            else throw new Exception("Server is already started!");
        }

        /// <summary>
        /// Client listener
        /// </summary>
        private void ListenForClients()
        {
            try
            {
                if (this.tcpListener is not null &&
                    this.connection is not null &&
                    this.command is not null)
                {
                    while (true)
                    {
                        // Wait for a client, thread is freezed until client connected.
                        TcpClient client = this.tcpListener.AcceptTcpClient();

                        // Client connected
                        Console.WriteLine("Client is connected.");

                        // !!! Here is happening all the magic <3 !!!
                        // Creating client hendler class
                        Client clientH = new(this, client, connection, command);

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
        /// Customer service
        /// </summary>
        /// <param name="server">
        /// Server
        /// </param>
        /// <param name="client">
        /// Client
        /// </param>
        /// <param name="connection">
        /// mySql connection
        /// </param>
        /// <param name="command">
        /// mySql comment
        /// </param>
        private partial class Client(Server server, TcpClient client, MySqlConnection connection, MySqlCommand command)
        {
            // Creating connection with a client
            protected readonly NetworkStream stream = client.GetStream();
            protected List<byte[]> byteBush = [];

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
        }
    }
}