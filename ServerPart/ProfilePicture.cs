using Newtonsoft.Json;
using System.Drawing;

namespace Connect.profilePicture
{
    /// <summary>
    /// An implementation of profile picture
    /// </summary>
    public class ProfilePicture
    {
        /// <summary>
        /// Current background of profile picture
        /// </summary>
        [JsonProperty("color")]
        public string PPColor { get; set; } = "default";
        /// <summary>
        /// Current picture name
        /// </summary>
        [JsonProperty("image_name")]
        public string PictureName { get; set; } = "default";
        /// <summary>
        /// Variable for packing and requesting
        /// Implamenting login of user
        /// </summary>
        [JsonProperty("login")]
        public string? Login { get; set; } = null;
    }
}