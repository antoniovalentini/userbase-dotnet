using System.Net;

namespace Userbase.Client.Ws.Errors
{
    public class PendingRequestError
    {
        public HttpStatusCode StatusCode { get; }
        public string Message { get; }
        public float RetryDelay { get; set; }

        public PendingRequestError(HttpStatusCode statusCode, string message)
        {
            StatusCode = statusCode;
            Message = message;
        }
    }
}
