using Newtonsoft.Json;

namespace Connect.message
{
    /// <summary>
    /// An implemintation of message 
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
}