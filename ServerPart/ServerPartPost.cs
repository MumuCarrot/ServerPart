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
            /// Post request
            /// </summary>
            /// <param name="request">
            /// Method with request
            /// </param>
            /// <exception cref="Exception">
            /// Method not found
            /// </exception>
            private void PostRequest(string request)
            {
                int methodIndex = request.IndexOf(' ');
                if (methodIndex == -1) throw new Exception("Post method was not found.");

                string methodWord = request[..methodIndex];
                switch (methodWord)
                {
                    case "--USER":
                        this.PostUser(request);
                        break; // --USER
                    case "--MSG":
                        this.PostMessage(request);
                        break; // --MSG
                }
            }

            private void PostUser(string request)
            {
                User? user = JsonExtractor<User>(request, "json", 0);

                if (user is not null)
                {
                    command = new($"INSERT INTO users VALUES (\'{user.Login}\', \'{user.Login}\', \'{user.Password}\');", connection);

                    using var reader = command.ExecuteReader();
                }
            }

            private void PostMessage(string request)
            {
                Message? message = JsonExtractor<Message>(request, "json", right:1);

                if (message is not null)
                { 
                    string content = message.Content?.Text ?? "";
                    if (content.Contains('\'')) 
                    {
                        content = message.Content?.Text?.Replace("\'", "&#cO") ?? "null";
                    }

                    /**
                     *  STAGE I
                     *  
                     *  { 
                     *      "_id": ObjectId('660afbf1b76620cd7544eefe') 
                     *  }
                     */

                    var filter = Builders<Chat>.Filter.Eq(c => c.Id, ObjectId.Parse("660afbf1b76620cd7544eefe"));

                    /**
                     *  STAGE II
                     * 
                     *  { 
                     *      $push: { 
                     *          "Messages": { 
                     *              "sender": "so4najaPopka19", 
                     *              "Content": { 
                     *                  "Text": "I love u too", 
                     *                  "Image": "" 
                     *              },
                     *              "Time": { 
                     *                  "$date": "2024-04-01T18:25:49.205Z" 
                     *                  }
                     *              }
                     */

                    var update = Builders<Chat>.Update.Push(c => c.Messages, message);

                    collection?.UpdateOne(filter, update);

                    string json = JsonConvert.SerializeObject(message);

                    this.SendGlobalMessage($"POST --MSG json{{{json}}}");
                }
            }
        }
    }
}
