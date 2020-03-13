using System;
using System.Net;
using Userbase.Client.Errors;

namespace Userbase.Client.Ws.Errors
{
    public class WebSocketError : Exception, IError
    {
        public string Name => "WebSocketError";
        public HttpStatusCode Status => HttpStatusCode.InternalServerError;
        public string Username { get; }
        public WebSocketError(string message, string username) : base(message)
        {
            Username = username;
        }
    }
}
