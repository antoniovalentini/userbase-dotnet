using System;
using System.Net;
using Userbase.Client.Errors;

namespace Userbase.Client.Ws.Errors
{
    public class RequestFailed : Exception, IError
    {
        public string Name { get; }
        public HttpStatusCode Status { get; }
        public RequestFailed(string action, HttpStatusCode statusCode, string message) : base(message)
        {
            Name = $"RequestFailed: {action}";
            Status = statusCode;
            // TODO: figure out what's going on with this js code
            /*
             * this.status = e.status || (e.message === 'timeout' && statusCodes['Gateway Timeout'])
             * this.response = e.status && e
             */
        }
    }
}
