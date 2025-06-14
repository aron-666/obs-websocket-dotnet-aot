using System.Text.Json;using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace OBSWebsocketDotNet.Types
{
    /// <summary>
    /// GetRecordingStatus response
    /// </summary>
    public class RecordingStatus
    {
        /// <summary>
        /// Current recording status
        /// </summary>
        [JsonPropertyName("outputActive")]
        public bool IsRecording { set; get; }

        /// <summary>
        /// Whether the recording is paused or not
        /// </summary>
        [JsonPropertyName("outputPaused")]
        public bool IsRecordingPaused { set; get; }

        /// <summary>
        /// Current formatted timecode string for the output
        /// </summary>
        [JsonPropertyName("outputTimecode")]
        public string RecordTimecode { set; get; }

        /// <summary>
        /// Current duration in milliseconds for the output
        /// </summary>
        [JsonPropertyName("outputDuration")]
        public long RecordingDuration { set; get; }

        /// <summary>
        /// Number of bytes sent by the output
        /// </summary>
        [JsonPropertyName("outputBytes")]
        public long RecordingBytes { set; get; }        /// <summary>
        /// Builds the object from the JSON response body
        /// </summary>
        /// <param name="data">JSON response body as a <see cref="JsonObject"/></param>
        public RecordingStatus(JsonObject data)
        {
            IsRecording = data["outputActive"]?.GetValue<bool>() ?? false;
            IsRecordingPaused = data["outputPaused"]?.GetValue<bool>() ?? false;
            RecordTimecode = data["outputTimecode"]?.GetValue<string>() ?? string.Empty;
            RecordingDuration = data["outputDuration"]?.GetValue<long>() ?? 0L;
            RecordingBytes = data["outputBytes"]?.GetValue<long>() ?? 0L;
        }

        /// <summary>
        /// Default Constructor for deserialization
        /// </summary>
        public RecordingStatus() { }
    }
}
