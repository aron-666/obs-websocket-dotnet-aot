using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OBSWebsocketDotNet.Communication
{
    internal static class MessageFactory
    {
        internal static JsonElement BuildMessage(MessageTypes opCode, string messageType, JsonElement? additionalFields, out string messageId)
        {
            messageId = Guid.NewGuid().ToString();
            
            var payload = new Dictionary<string, object>
            {
                { "op", (int)opCode }
            };

            var data = new Dictionary<string, object>();
            
            switch (opCode)
            {
                case MessageTypes.Request:
                    data.Add("requestType", messageType);
                    data.Add("requestId", messageId);
                    if (additionalFields.HasValue)
                    {
                        data.Add("requestData", JsonHelper.ToDictionary(additionalFields.Value));
                    }
                    break;
                case MessageTypes.RequestBatch:
                    data.Add("requestId", messageId);
                    break;
                case MessageTypes.Identify:
                    if (additionalFields.HasValue)
                    {
                        var additionalDict = JsonHelper.ToDictionary(additionalFields.Value);
                        foreach (var kvp in additionalDict)
                        {
                            data[kvp.Key] = kvp.Value;
                        }
                    }
                    break;
            }

            payload.Add("d", data);
            return JsonHelper.ToJsonElement(payload);
        }
    }
}
