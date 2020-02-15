using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Userbase.Client.Models;

namespace Userbase.Client
{
    public class WebSocketError : Exception
    {
        public string Username { get; }

        public WebSocketError(string message, string username) : base(message)
        {
            Username = username;
        }
    }

    public class Ws
    {
        private readonly Config _config;
        private const string WsAlreadyConnected = "Web Socket already connected";

        public bool Connected { get; private set; } = false;
        public Session Session { get; set; }
        public string ClientId { get; }

        public Ws(Config config)
        {
            _config = config;
            ClientId = Guid.NewGuid().ToString();
        }

        public async Task SignOut()
        {
            // TODO
            await Task.FromResult(true);
        }

        public async Task<HttpResponseMessage> Connect(SignInSession session, string seed, string rememberMe, string username)
        {
            // TODO
            if (Connected) throw new WebSocketError(WsAlreadyConnected, username);


            var webSocket = new ClientWebSocket();
            var uri = new Uri($"{WsUtils.GetWsUrl(_config.Endpoint)}/api?appId=${_config.AppId}&sessionId=${session.SessionId}&clientId=${ClientId}");
            await webSocket.ConnectAsync(uri, CancellationToken.None);

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }

    public class Session
    {
        public string Username { get; set; }
    }
}
