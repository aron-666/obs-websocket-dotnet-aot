using System.Text.Json;
using System.Text.Json.Serialization;

namespace OBSWebsocketDotNet.Types
{
    /// <summary>
    /// Settings for a source item
    /// </summary>
    public class InputSettings : Input
    {
        /// <summary>
        /// Settings for the source
        /// </summary>
        [JsonPropertyName("inputSettings")]
        public JsonElement Settings { set; get; }

        /// <summary>
        /// Builds the object from the JSON data
        /// </summary>
        /// <param name="data">JSON item description as a <see cref="JsonElement"/></param>
        public InputSettings(JsonElement data) : base(data)
        {
            // Converted to use JsonHelper
        }

        /// <summary>
        /// Default Constructor for deserialization
        /// </summary>
        public InputSettings() { }
    }
}
