using System.Text.Json;
using System.Text.Json.Serialization;

namespace OBSWebsocketDotNet.Communication
{
    /// <summary>
    /// Message received from the server
    /// </summary>
    internal class ServerMessage
    {
        /// <summary>
        /// Server Message's operation code
        /// </summary>
        [JsonPropertyName("op")]
        public MessageTypes OperationCode { get; set; }

        /// <summary>
        /// Server Data
        /// </summary>
        [JsonPropertyName("d")]
        public JsonElement Data { get; set; }
    }
}
