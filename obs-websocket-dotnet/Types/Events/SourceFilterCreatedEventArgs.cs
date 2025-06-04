using System;
using System.Text.Json;
using OBSWebsocketDotNet;

namespace OBSWebsocketDotNet.Types.Events
{
    /// <summary>
    /// Event args for <see cref="OBSWebsocket.SourceFilterCreated"/>
    /// </summary>
    public class SourceFilterCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the source the filter was added to
        /// </summary>
        public string SourceName { get; }
        
        /// <summary>
        /// Name of the filter
        /// </summary>
        public string FilterName{ get; }
        
        /// <summary>
        /// The kind of the filter
        /// </summary>
        public string FilterKind{ get; }
        
        /// <summary>
        /// Index position of the filter
        /// </summary>
        public int FilterIndex{ get; }
        
        /// <summary>
        /// The settings configured to the filter when it was created
        /// </summary>
        public JsonElement FilterSettings{ get; }
        
        /// <summary>
        /// The default settings for the filter
        /// </summary>
        public JsonElement DefaultFilterSettings { get; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="sourceName">The source name</param>
        /// <param name="filterName">The filter name</param>
        /// <param name="filterKind">The kind of filter</param>
        /// <param name="filterIndex">The index of the filter</param>
        /// <param name="filterSettings">The filters settings as a JsonElement</param>
        /// <param name="defaultFilterSettings">The default filter settings as a JsonElement</param>
        public SourceFilterCreatedEventArgs(string sourceName, string filterName, string filterKind, int filterIndex, JsonElement filterSettings, JsonElement defaultFilterSettings)
        {
            SourceName = sourceName;
            FilterName = filterName;
            FilterKind = filterKind;
            FilterIndex = filterIndex;
            FilterSettings = filterSettings;
            DefaultFilterSettings = defaultFilterSettings;
        }
    }
}

