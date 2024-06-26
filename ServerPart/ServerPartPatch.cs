﻿using Connect.profilePicture;
using Connect.user;

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
                    case "--PROFILE-PIC":
                        PatchProfilePicture(request);
                        break; // --PROFILE-PIC
                }
            }

            /// <summary>
            /// Update current user
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void PatchUpdateUser(string request)
            {
                User? user = null;
                int shift = 4;
                bool readed = false;
                while (!readed) 
                { 
                    try 
                    { 
                        user = JsonExtractor<User>(request, "user", right:shift);
                        readed = true;
                    }
                    catch 
                    {
                        shift--;
                        if (shift < 0) throw new Exception();
                    }
                }

                if (user is not null)
                {
                    command = new($"UPDATE users " +
                                  $"SET username = \"{user.UserName}\", user_login = \"{user.Login}\", about_me = \"{user.AboutMe}\" " +
                                  $"WHERE user_login = '{user.Login}';", connection);

                    using var reader = command.ExecuteReader();
                }
            }

            /// <summary>
            /// Update password of current user
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void PatchUserPassword(string request)
            {
                string?[]? str = JsonExtractor<string?[]?>(request, "json");
                if (str is not null && str[0] is not null && str[1] is not null)
                {
                    command = new($"UPDATE users " +
                                  $"SET user_password = \"{str[0]}\" " +
                                  $"WHERE user_login = \"{str[1]}\";", connection);

                    using var reader = command.ExecuteReader();
                }
            }

            /// <summary>
            /// Update profile picture of current user
            /// </summary>
            /// <param name="request">
            /// Request
            /// </param>
            private void PatchProfilePicture(string request)
            {
                ProfilePicture? profilePicture = JsonExtractor<ProfilePicture>(request, "user", right: 1);

                if (profilePicture is not null)
                {
                    command = new($"UPDATE users " +
                                  $"SET profile_picture = \"{profilePicture.PictureName}\", " +
                                  $"profile_background = \"{profilePicture.PPColor}\" " +
                                  $"WHERE user_login = \"{profilePicture.Login}\";", connection);

                    using var reader = command.ExecuteReader();
                }
            }
        }
    }
}
