using System;
using System.Net;

namespace Userbase.Client.Ws.Models
{
    public class WsRequest
    {
        public Action<string> Resolve { get; set; }
        public Action<string, string, HttpStatusCode, string> Reject { get; set; }
    }
}
