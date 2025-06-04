using System.Text.Json;
using System.Text.Json.Serialization;

namespace OBSWebsocketDotNet.Types
{
    /// <summary>
    /// Source volume values
    /// </summary>
    public class InputVolume
    {
        /// <summary>
        /// Name of the source
        /// </summary>
        [JsonPropertyName("inputName")]
        public string InputName { set; get; }
        /// <summary>
        /// The source volume in percent
        /// </summary>
        [JsonPropertyName("inputVolumeMul")]
        public float InputVolumeMul { get; set; }
        /// <summary>
        /// The source volume in decibels
        /// </summary>
        [JsonPropertyName("inputVolumeDb")]
        public float InputVolumeDb { get; set; }        /// <summary>
        /// Builds the object from the JSON response body
        /// </summary>
        /// <param name="data">JSON response body as a <see cref="JsonElement"/></param>
        public InputVolume(JsonElement data)
        {
            InputName = JsonHelper.GetPropertyValue<string>(data, "inputName");
            InputVolumeMul = JsonHelper.GetPropertyValue<float>(data, "inputVolumeMul");
            InputVolumeDb = JsonHelper.GetPropertyValue<float>(data, "inputVolumeDb");
        }

        /// <summary>
        /// Empty constructor for jsonconvert
        /// </summary>
        public InputVolume() { }
    }
}
