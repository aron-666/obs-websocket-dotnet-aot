using System;

namespace OBSWebsocketDotNet
{
    /// <summary>
    /// Thrown if authentication fails
    /// </summary>
    public class AuthFailureException : Exception
    {
    }    /// <summary>
    /// Thrown when the server responds with an error
    /// </summary>
    public class ErrorResponseException : Exception
    {
        /// <summary>
        /// Error Code of exception
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception Message</param>
        /// /// <param name="errorCode">Error Code</param>
        public ErrorResponseException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Thrown when the websocket is not connected
    /// </summary>
    public class ObsWebsocketNotConnectedException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ObsWebsocketNotConnectedException() : base("Websocket is not connected")
        {
        }
    }

    /// <summary>
    /// Thrown when an OBS websocket error occurs
    /// </summary>
    public class ObsWebsocketException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        public ObsWebsocketException(string message) : base(message)
        {
        }
    }
}