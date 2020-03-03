using System;

namespace Userbase.Client.Ws.Errors
{
    public class WebSocketError : Exception
    {
        public string Username { get; }
        public WebSocketError(string message, string username) : base(message)
        {
            Username = username;
        }
    }
}
