using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using Userbase.Client.Models;
using WebSocket4Net;

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
        private const int BackoffRetryDelay = 1000;

        public static WebSocket Instance4Net;
        
        private readonly Config _config;
        private const string WsAlreadyConnected = "Web Socket already connected";

        public bool Connected { get; set; } = false;
        public bool Reconnecting { get; set; }
        public Session Session { get; set; }
        public string ClientId { get; }

        public Ws(Config config)
        {
            _config = config;
            ClientId = Guid.NewGuid().ToString();
        }

        public void Init()
        {

        }

        public async Task SignOut()
        {
            // TODO
            await Task.FromResult(true);
        }

        // TODO: missing currentState input param
        public async Task Reconnect(SignInSession session, string seed, string rememberMe, int reconnectDelay = 0)
        {
            await Task.FromException(new NotImplementedException());
        }

        public async Task<HttpResponseMessage> Connect(SignInSession session, string seed, string rememberMe, string username, int reconnectDelay = 0)
        {
            // TODO
            if (Connected) throw new WebSocketError(WsAlreadyConnected, username);

            var url = $"{WsUtils.GetWsUrl(_config.Endpoint)}api?appId={_config.AppId}&sessionId={session.SessionId}&clientId={ClientId}";

            // TODO: handle timeouts
            Instance4Net = new WebSocket(url);
            Instance4Net.Opened += OnOpened;
            Instance4Net.DataReceived += OnDataReceived;
            Instance4Net.MessageReceived += OnMessageReceived;
            Instance4Net.Closed += async (sender, args) =>
            {
                await OnClosed(sender, args, session, seed, rememberMe, username, reconnectDelay);
            };
            Instance4Net.Error += OnError;
            var result = await Instance4Net.OpenAsync();
            
            return result
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        private void OnOpened(object sender, EventArgs e)
        {
            //if (timeout) return;
            //clearTimeout(timeoutToOpenWebSocket);
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Hello!");
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var msg = Newtonsoft.Json.Linq.JObject.Parse(e.Message);
            if (msg["route"]?.ToString().ToLower() == "ping")
            {
                Instance4Net.Send(Newtonsoft.Json.JsonConvert.SerializeObject("Pong"));
            }

            Console.WriteLine(e.Message);
        }

        private async Task OnClosed(object sender, EventArgs args, SignInSession session, string seed, string rememberMe, string username, int reconnectDelay)
        {
            //if (timeout) return;
            Console.WriteLine(sender);
            if (args is ClosedEventArgs e)
            {
                var serviceRestart = e.Code == 1012; // statusCodes['Service Restart']
                var clientDisconnected = e.Code == 3000; //statusCodes['No Pong Received']
                // TODO: investigate on e.wasClean
                var attemptToReconnect =
                    serviceRestart ||
                    clientDisconnected; // || !e.wasClean // closed without explicit call to ws.close()

                if (attemptToReconnect) {
                    var delay = serviceRestart && reconnectDelay <= 0
                        ? 0
                        : (reconnectDelay > 0 ? reconnectDelay + BackoffRetryDelay : 1000);

                    Reconnecting = true;
                    await Reconnect(session, seed, rememberMe, delay);
                } 
                else if (e.Code == 3001 /*statusCodes['Client Already Connected']*/) {
                    throw new WebSocketError(WsAlreadyConnected, username);
                } 
                else
                {
                    Init();
                }
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }
    }

    // TODO: this is temporary only
    public class Session
    {
        public string Username { get; set; }
    }
}
