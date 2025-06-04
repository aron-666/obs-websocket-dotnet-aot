using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OBSWebsocketDotNet
{
    /// <summary>
    /// Helper class for JSON operations to replace JsonElement functionality
    /// </summary>
    internal static class JsonHelper
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }

        /// <summary>
        /// Deserialize JsonElement to object
        /// </summary>
        public static T Deserialize<T>(JsonElement element)
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText(), DefaultOptions);
        }

        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, DefaultOptions);
        }

        /// <summary>
        /// Parse JSON string to JsonDocument
        /// </summary>
        public static JsonDocument Parse(string json)
        {
            return JsonDocument.Parse(json);
        }

        /// <summary>
        /// Create JsonElement from object
        /// </summary>
        public static JsonElement ToJsonElement<T>(T obj)
        {
            var json = Serialize(obj);
            using var doc = Parse(json);
            return doc.RootElement.Clone();
        }

        /// <summary>
        /// Try get property value from JsonElement
        /// </summary>
        public static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            return element.TryGetProperty(propertyName, out value);
        }

        /// <summary>
        /// Get property value from JsonElement with default
        /// </summary>
        public static T GetPropertyValue<T>(JsonElement element, string propertyName, T defaultValue = default)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                try
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)property.GetString();
                    if (typeof(T) == typeof(int))
                        return (T)(object)property.GetInt32();
                    if (typeof(T) == typeof(long))
                        return (T)(object)property.GetInt64();
                    if (typeof(T) == typeof(bool))
                        return (T)(object)property.GetBoolean();
                    if (typeof(T) == typeof(double))
                        return (T)(object)property.GetDouble();
                    if (typeof(T) == typeof(float))
                        return (T)(object)property.GetSingle();
                    
                    return JsonSerializer.Deserialize<T>(property.GetRawText(), DefaultOptions);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }        /// <summary>
        /// Create a dictionary from JsonElement
        /// </summary>
        public static Dictionary<string, object> ToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            
            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.TryGetInt32(out var intVal) ? intVal : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Array => property.Value.EnumerateArray().Select(x => (object)x).ToList(),
                    JsonValueKind.Object => ToDictionary(property.Value),
                    _ => null
                };
            }
            
            return dict;
        }

        /// <summary>
        /// Create JsonElement from dictionary
        /// </summary>
        public static JsonElement CreateJsonElement(Dictionary<string, object> dict)
        {
            return ToJsonElement(dict);
        }

        /// <summary>
        /// Get string value from JsonElement property
        /// </summary>
        public static string GetStringValue(JsonElement element, string propertyName)
        {
            return GetPropertyValue<string>(element, propertyName);
        }

        /// <summary>
        /// Get int value from JsonElement property
        /// </summary>
        public static int GetIntValue(JsonElement element, string propertyName)
        {
            return GetPropertyValue<int>(element, propertyName);
        }

        /// <summary>
        /// Get bool value from JsonElement property
        /// </summary>
        public static bool GetBoolValue(JsonElement element, string propertyName)
        {
            return GetPropertyValue<bool>(element, propertyName);
        }

        /// <summary>
        /// Get double value from JsonElement property
        /// </summary>
        public static double GetDoubleValue(JsonElement element, string propertyName)
        {
            return GetPropertyValue<double>(element, propertyName);
        }

        /// <summary>
        /// Check if JsonElement has property
        /// </summary>
        public static bool HasProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out _);
        }
    }
}

