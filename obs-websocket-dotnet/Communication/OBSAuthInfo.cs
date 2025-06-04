using System.Text.Json;
using System.Text.Json.Serialization;

namespace OBSWebsocketDotNet.Communication
{
    /// <summary>
    /// Data required by authentication
    /// </summary>
    public class OBSAuthInfo
    {
        /// <summary>
        /// Authentication challenge
        /// </summary>
        [JsonPropertyName("challenge")]
        public string Challenge { get; set; }

        /// <summary>
        /// Password salt
        /// </summary>
        [JsonPropertyName("salt")]
        public string PasswordSalt { get; set; }

        /// <summary>
        /// Builds the object from JSON response body
        /// </summary>
        /// <param name="data">JSON response body as a <see cref="JsonElement"/></param>
        public OBSAuthInfo(JsonElement data)
        {
            if (data.TryGetProperty("authentication", out var authElement))
            {
                Challenge = JsonHelper.GetPropertyValue<string>(authElement, "challenge");
                PasswordSalt = JsonHelper.GetPropertyValue<string>(authElement, "salt");
            }
        }

        /// <summary>
        /// Default Constructor for deserialization
        /// </summary>
        public OBSAuthInfo() { }
    }
}