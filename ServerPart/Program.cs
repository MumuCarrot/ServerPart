/**
 * Ivan Kovalenko
 * 10.03.2024
 */

using Connect.server;

namespace Connect.main
{
    /// <summary>
    /// Program entrence point
    /// </summary>
    class Program
    {
        /// <summary>
        /// Entrence
        /// </summary>
        static void Main()
        {
            var server = new Server("127.0.0.1", 7007);

            server.SetDBPath("server=localhost;uid=root;pwd=1234;database=mydb;");

            server.Start();
        }
    }
}