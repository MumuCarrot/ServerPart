using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace Connect.message
{
    public class Chat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        public string[] Chatusers { get; set; } = new string[0];

        public Message[] Messages { get; set; } = new Message[0];
    }

    public class Message
    {
        [BsonElement("Sender")]
        public string? Username { get; set; }

        public ContentMessage? Content { get; set; }

        public DateTime? Time { get; set; }
    }

    public class ContentMessage
    {
        public string? Text { get; set; }

        public string? Image { get; set; }
    }
}