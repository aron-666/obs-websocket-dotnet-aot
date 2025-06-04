using System.Text.Json;
using System.Text.Json.Serialization;

namespace OBSWebsocketDotNet.Types
{
    /// <summary>
    /// Streaming settings
    /// </summary>
    public class StreamingService
    {
        /// <summary>
        /// Type of streaming service
        /// </summary>
        [JsonPropertyName("streamServiceType")]
        public string Type { set; get; }        /// <summary>
        /// Streaming service settings (JSON data)
        /// </summary>
        [JsonPropertyName("streamServiceSettings")]
        public StreamingServiceSettings Settings { set; get; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public StreamingService() { }

        /// <summary>
        /// Constructor from JsonElement
        /// </summary>
        /// <param name="element">JsonElement containing streaming service data</param>
        public StreamingService(JsonElement element)
        {
            if (element.TryGetProperty("streamServiceType", out var typeElement))
            {
                Type = typeElement.GetString();
            }

            if (element.TryGetProperty("streamServiceSettings", out var settingsElement))
            {
                Settings = JsonHelper.Deserialize<StreamingServiceSettings>(settingsElement);
            }
        }
    }
}
