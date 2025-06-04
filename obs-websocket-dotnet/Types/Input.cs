using System.Text.Json;
using System.Text.Json.Serialization;

namespace OBSWebsocketDotNet.Types
{
    /// <summary>
    /// Abstract class with information on a specific Input
    /// </summary>
    public abstract class Input
    {
        /// <summary>
        /// Name of the Input
        /// </summary>
        [JsonPropertyName("inputName")]
        public string InputName { get; set; }

        /// <summary>
        /// Kind of the Input
        /// </summary>
        [JsonPropertyName("inputKind")]
        public string InputKind { get; set; }

        /// <summary>
        /// Instantiate object from response data
        /// </summary>
        /// <param name="body"></param>
        public Input(JsonElement body)
        {
            InputName = JsonHelper.GetPropertyValue<string>(body, "inputName");
            InputKind = JsonHelper.GetPropertyValue<string>(body, "inputKind");
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Input() { }
    }
}
