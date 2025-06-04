using System;
using System.Text.Json;

namespace OBSWebsocketDotNet.Types.Events
{
    /// <summary>
    /// Event args for unsupported events
    /// </summary>
    public class UnsupportedEventArgs : EventArgs
    {
        /// <summary>
        /// The type of the event
        /// </summary>
        public string EventType { get; }
        /// <summary>
        /// The body of the event
        /// </summary>
        public JsonElement Body { get; }

        /// <summary>
        /// Event args for unsupported events
        /// </summary>
        public UnsupportedEventArgs(string eventType, JsonElement body)
        {
            EventType = eventType;
            Body = body;
        }
    }
}
