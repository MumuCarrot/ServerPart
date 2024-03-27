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
            private void PatchUpdateUser(string request)
            {
                User? user = JsonExtractor<User>(request, "json", 0);

                if (user is not null)
                {
                    command = new($"UPDATE users SET username = \"{user.UserName}\", user_login = \"{user.Login}\", about_me = \"{user.AboutMe}\" WHERE user_login = '{user.Login}';", connection);

                    using var reader = command.ExecuteReader();
                }
            }

            private void PatchUserProfilePicture(string request)
            {
                if (request.Contains("avatar"))
                {
                    byteBush = [];

                    ava = JsonExtractor<ProfilePicture>(request, "json", 0);

                    SendMessage($"PATCH --UPD_AVATAR part{{status{{ready}}}}");
                }
                else if (request.Contains("part"))
                {
                    byte[]? bytes = JsonExtractor<byte[]>(request, "part", 0);

                    if (bytes is not null)
                    {
                        byteBush.Add(bytes);

                        SendMessage($"PATCH --UPD_AVATAR part{{status{{ready}}}}");

                        Console.WriteLine("Part");
                    }
                }
                else if (request.Contains("close"))
                {
                    if (ava is not null)
                    {
                        Console.WriteLine("Close");

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
