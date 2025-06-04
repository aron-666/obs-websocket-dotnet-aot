using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OBSWebsocketDotNet.Communication;
using Websocket.Client;

namespace OBSWebsocketDotNet
{
    public partial class OBSWebsocket : IOBSWebsocket
    {
        #region Private Members
        private const string WEBSOCKET_URL_PREFIX = "ws://";
        private const int SUPPORTED_RPC_VERSION = 1;
        private TimeSpan wsTimeout = TimeSpan.FromSeconds(10);
        private string connectionPassword = null;
        private WebsocketClient wsConnection;

        private delegate void RequestCallback(OBSWebsocket sender, JsonElement body);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> responseHandlers;

        // Random should never be created inside a function
        private static readonly Random random = new Random();

        #endregion

        /// <summary>
        /// WebSocket request timeout, represented as a TimeSpan object
        /// </summary>
        public TimeSpan WSTimeout
        {
            get
            {
                return wsConnection?.ReconnectTimeout ?? wsTimeout;
            }
            set
            {
                wsTimeout = value;

                if (wsConnection != null)
                {
                    wsConnection.ReconnectTimeout = wsTimeout;
                }
            }
        }
      
        /// <summary>
        /// Current connection state
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return (wsConnection != null && wsConnection.IsRunning);
            }
        }

        /// <summary>
        /// Gets or sets the logger for this instance
        /// </summary>
        public ILogger<OBSWebsocket> Logger { get; set; } = NullLogger<OBSWebsocket>.Instance;

        /// <summary>
        /// Constructor
        /// </summary>
        public OBSWebsocket()
        {
            responseHandlers = new ConcurrentDictionary<string, TaskCompletionSource<JsonElement>>();
        }

        /// <summary>
        /// Connect this instance to the specified URL, and authenticate (if needed) with the specified password.
        /// NOTE: Please subscribe to the Connected/Disconnected events (or atleast check the IsConnected property) to determine when the connection is actually fully established
        /// </summary>
        /// <param name="url">Server URL in standard URL format.</param>
        /// <param name="password">Server password</param>
        [Obsolete("Please use ConnectAsync, this function will be removed in the next version")]
        public void Connect(string url, string password)
        {
            ConnectAsync(url, password);
        }

        /// <summary>
        /// Connect this instance to the specified URL, and authenticate (if needed) with the specified password.
        /// NOTE: Please subscribe to the Connected/Disconnected events (or atleast check the IsConnected property) to determine when the connection is actually fully established
        /// </summary>
        /// <param name="url">Server URL in standard URL format.</param>
        /// <param name="password">Server password</param>
        public void ConnectAsync(string url, string password)
        {
            if (!url.ToLower().StartsWith(WEBSOCKET_URL_PREFIX))
            {
                throw new ArgumentException($"Invalid url, must start with '{WEBSOCKET_URL_PREFIX}'");
            }

            if (wsConnection != null && wsConnection.IsRunning)
            {
                return;
            }

            connectionPassword = password;

            wsConnection = new WebsocketClient(new Uri(url))
            {
                ReconnectTimeout = wsTimeout
            };            wsConnection.ReconnectionHappened.Subscribe(info => OnWebsocketConnect(info));
            wsConnection.MessageReceived.Subscribe(msg => WebsocketMessageHandler(msg));
            wsConnection.DisconnectionHappened.Subscribe(info => OnWebsocketDisconnect(info));

            wsConnection.Start();
        }

        /// <summary>
        /// Disconnect this instance from the server
        /// </summary>
        public void Disconnect()
        {
            wsConnection?.Stop(WebSocketCloseStatus.NormalClosure, "User disconnected");
        }

        // This callback handles a websocket connection established
        private void OnWebsocketConnect(ReconnectionInfo reconnectionInfo)
        {
            Logger?.LogInformation("OBS Websocket connection established");
        }        // This callback handles a websocket disconnection
        private void OnWebsocketDisconnect(DisconnectionInfo d)
        {
            Logger?.LogInformation($"OBS Websocket disconnection, info: {d.Type} - {d.CloseStatus} - {d.CloseStatusDescription}");

            ObsCloseCodes obsCloseCode = ObsCloseCodes.UnknownReason;
            if (d.CloseStatus.HasValue)
            {
                obsCloseCode = (ObsCloseCodes)d.CloseStatus.Value;
            }

            Disconnected?.Invoke(this, new ObsDisconnectionInfo(obsCloseCode, d.CloseStatusDescription ?? "Unknown reason", d));
        }        // This callback handles incoming JSON messages and determines if it's
        // a request response or an event ("Update" in obs-websocket terminology)
        private void WebsocketMessageHandler(ResponseMessage e)
        {
            if (e.MessageType != WebSocketMessageType.Text)
            {
                return;
            }

            ServerMessage msg = JsonSerializer.Deserialize<ServerMessage>(e.Text);
            JsonElement body = msg.Data;

            switch (msg.OperationCode)
            {
                case MessageTypes.Hello:
                    // First message received after connection, this may ask us for authentication
                    HandleHello(body);
                    break;
                case MessageTypes.Identified:
                    Task.Run(() => Connected?.Invoke(this, EventArgs.Empty));
                    break;
                case MessageTypes.RequestResponse:
                case MessageTypes.RequestBatchResponse:
                    // Handle response to previous request
                    if (body.TryGetProperty("requestId", out var requestIdElement))
                    {
                        // Handle a request :
                        // Find the response handler based on
                        // its associated message ID
                        string msgID = requestIdElement.GetString();

                        if (responseHandlers.TryRemove(msgID, out TaskCompletionSource<JsonElement> handler))
                        {
                            // Set the response body as Result and notify the request sender
                            handler.SetResult(body);
                        }
                    }
                    break;
                case MessageTypes.Event:
                    // Handle events
                    string eventType = body.GetProperty("eventType").GetString();
                    Task.Run(() => { ProcessEventType(eventType, body); });
                    break;
                default:
                    // Unsupported message type
                    Logger?.LogWarning($"Unsupported message type: {msg.OperationCode}");
                    UnsupportedEvent?.Invoke(this, new Types.Events.UnsupportedEventArgs(msg.OperationCode.ToString(), body));
                    break;
            }
        }

        /// <summary>
        /// Sends a message to the websocket API with the specified request type and optional parameters
        /// </summary>
        /// <param name="requestType">obs-websocket request type, must be one specified in the protocol specification</param>
        /// <param name="additionalFields">additional JSON fields if required by the request type</param>
        /// <returns>The server's JSON response as a JsonElement</returns>
        public JsonElement SendRequest(string requestType, JsonElement? additionalFields = null)
        {
            return SendRequest(MessageTypes.Request, requestType, additionalFields, true);
        }

        /// <summary>
        /// Internal version which allows to set the opcode
        /// Sends a message to the websocket API with the specified request type and optional parameters
        /// </summary>
        /// <param name="operationCode">Type/OpCode for this messaage</param>
        /// <param name="requestType">obs-websocket request type, must be one specified in the protocol specification</param>
        /// <param name="additionalFields">additional JSON fields if required by the request type</param>
        /// <param name="waitForReply">Should wait for reply vs "fire and forget"</param>
        /// <returns>The server's JSON response as a JsonElement</returns>
        internal JsonElement SendRequest(MessageTypes operationCode, string requestType, JsonElement? additionalFields = null, bool waitForReply = true)
        {
            if (!IsConnected)
                throw new ObsWebsocketNotConnectedException();

            JsonElement message;
            string messageId;
            
            // Prepare the asynchronous response handler
            var tcs = new TaskCompletionSource<JsonElement>();
            try
            {
                // Generate a random message id
                message = MessageFactory.BuildMessage(operationCode, requestType, additionalFields, out messageId);
                if (waitForReply)
                {
                    // Register the request callback
                    responseHandlers[messageId] = tcs;
                }

                // Send the message and wait for a response
                // (received and notified by the websocket response handler)
                wsConnection.Send(message.GetRawText());

                if (waitForReply)
                {
                    var result = tcs.Task.Result;

                    if (!JsonHelper.GetPropertyValue<bool>(result.GetProperty("requestStatus"), "result", false))
                    {
                        string message1 = JsonHelper.GetPropertyValue<string>(result.GetProperty("requestStatus"), "comment", "Unknown Error");
                        throw new ObsWebsocketException(message1);
                    }

                    if (result.TryGetProperty("responseData", out var responseData)) // ResponseData is optional
                        return responseData;

                    return new JsonElement();
                }
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }

            return new JsonElement();
        }

        /// <summary>
        /// Request authentication data. You don't have to call this manually.
        /// </summary>
        /// <returns>AuthInfo object</returns>
        public OBSAuthInfo GetAuthInfo()
        {
            var msg = SendRequest("GetAuthRequired");
            return new OBSAuthInfo(msg);
        }

        /// <summary>
        /// Authenticates to the server using the challenge and salt given in the passed <see cref="OBSAuthInfo"/> object
        /// </summary>
        /// <param name="password">User password</param>
        /// <param name="authInfo">Authentication data</param>
        protected void SendIdentify(string password, OBSAuthInfo authInfo = null)
        {
            var responseBuilder = new Dictionary<string, object>
            {
                { "rpcVersion", SUPPORTED_RPC_VERSION }
            };

            if (!string.IsNullOrEmpty(password) && authInfo != null)
            {
                string secret = HashEncode(password + authInfo.PasswordSalt);
                string authResponse = HashEncode(secret + authInfo.Challenge);

                responseBuilder["authentication"] = authResponse;
            }

            var response = JsonHelper.ToJsonElement(responseBuilder);
            var identify = MessageFactory.BuildMessage(MessageTypes.Identify, string.Empty, response, out _);
            wsConnection.Send(identify.GetRawText());
        }

        /// <summary>
        /// Update session parameters. Called by the websocket client after a successful connection to obs-websocket.
        /// </summary>
        /// <param name="body">JSON body of the response</param>
        protected void HandleHello(JsonElement body)
        {
            var authInfo = new OBSAuthInfo(body);

            if (JsonHelper.GetPropertyValue<bool>(body, "authRequired", false))
            {
                // Authentication required
                SendIdentify(connectionPassword, authInfo);
            }
            else
            {
                // No authentication required
                SendIdentify(connectionPassword);
            }
        }

        /// <summary>
        /// Encode a Base64-encoded SHA256 hash
        /// </summary>
        /// <param name="input">source string</param>
        /// <returns></returns>
        protected static string HashEncode(string input)
        {
            var sha256 = SHA256.Create();

            byte[] textBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(textBytes);

            string hash = Convert.ToBase64String(hashBytes);
            return hash;
        }

        /// <summary>
        /// Generate a 32-character random string, used as a private session token
        /// </summary>
        /// <returns>A random 32-character hexadecimal string</returns>
        protected static string NewSessionToken()
        {
            const string pool = "abcdef0123456789";
            var sb = new StringBuilder();

            for (int i = 0; i < 32; i++)
            {
                char c = pool[random.Next(0, pool.Length)];
                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
