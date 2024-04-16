using Connect.profilePicture;
using Connect.user;
using System.Drawing;
using System.Drawing.Imaging;

namespace Connect.server
{
    public partial class Server
    {
        private partial class Client
        {
            /// <summary>
            /// Patch request
            /// </summary>
            /// <param name="request">
            /// Method with request
            /// </param>
            /// <exception cref="Exception">
            /// Method not found
            /// </exception>
            private void PatchRequest(string request)
            {
                int methodIndex = request.IndexOf(' ');
                if (methodIndex == -1) throw new Exception("Patch method was not found.");

                string methodWord = request[..methodIndex];
                switch (methodWord)
                {
                    case "--UPD_USER":
                        PatchUpdateUser(request);
                        break; // --UPD_USER
                    case "--UPD_UPASSWORD":
                        PatchUserPassword(request);
                        break; // --UPD_UPASSWORD
                }
            }

            private void PatchUpdateUser(string request)
            {
                User? user = JsonExtractor<User>(request, "json", 0);

                if (user is not null)
                {
                    command = new($"UPDATE users SET username = \"{user.UserName}\", user_login = \"{user.Login}\", about_me = \"{user.AboutMe}\" WHERE user_login = '{user.Login}';", connection);

                    using var reader = command.ExecuteReader();
                }
            }

            private void PatchUserPassword(string request)
            {
                string?[]? str = JsonExtractor<string?[]?>(request, "json", 0);
                if (str is not null && str[0] is not null && str[1] is not null)
                {
                    command = new($"UPDATE users SET user_password = \"{str[0]}\" WHERE user_login = \"{str[1]}\";", connection);

                    using var reader = command.ExecuteReader();
                }
            }
        }
    }
}
