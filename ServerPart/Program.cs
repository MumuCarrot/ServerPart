using Connect.server;

namespace Connect.main
{
    /// <summary>
    /// Program entrance point
    /// </summary>
    class Program
    {
        /// <summary>
        /// Entrance
        /// </summary>
        static void Main()
        {
            var server = new Server("127.0.0.1", 7007);

            server.SetDBPath("server=localhost;uid=root;pwd=1234;database=mydb;", "mongodb://localhost:27017");

            server.Start();
        }
    }
}