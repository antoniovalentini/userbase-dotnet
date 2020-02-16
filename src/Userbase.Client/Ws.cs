using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using Userbase.Client.Api;
using Userbase.Client.Crypto;
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
        private readonly AuthApi _api;
        private readonly Keys _keys = new Keys();
        private byte[] _encryptedValidationMessage;
        private string _seedString;

        private const string WsAlreadyConnected = "Web Socket already connected";

        public bool Connected { get; set; }
        public bool Reconnecting { get; set; }
        public Session Session { get; set; }
        public string ClientId { get; }

        public Ws(Config config, AuthApi api)
        {
            _config = config;
            _api = api;
            ClientId = Guid.NewGuid().ToString();
        }

        public void Init(string seedString)
        {
            _seedString = seedString;
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

        public async Task<HttpResponseMessage> Connect(SignInSession session, string seedString, string rememberMe, string username, int reconnectDelay = 0)
        {
            // TODO
            if (Connected) throw new WebSocketError(WsAlreadyConnected, username);

            var url = $"{WsUtils.GetWsUrl(_config.Endpoint)}api?appId={_config.AppId}&sessionId={session.SessionId}&clientId={ClientId}";

            // TODO: handle timeouts
            var webSocket = new WebSocket(url);
            webSocket.Opened += OnOpened;
            webSocket.DataReceived += OnDataReceived;
            webSocket.MessageReceived += async (sender, args) => await OnMessageReceived(sender, args, webSocket, seedString);
            webSocket.Closed += async (sender, args) => await OnClosed(sender, args, session, seedString, rememberMe, username, reconnectDelay);
            webSocket.Error += OnError;
            var result = await webSocket.OpenAsync();
            
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

        private async Task OnMessageReceived(object sender, MessageReceivedEventArgs e, WebSocket webSocket, string seedString)
        {
            Console.WriteLine(sender);
            var msg = JObject.Parse(e.Message);
            var route = msg["route"]?.ToString().ToLower() ?? "";

            switch (route)
            {
                case "connection":
                    Init(seedString);
                    Instance4Net = webSocket;
                    //this.heartbeat()
                    Connected = true;

                    var connectionMessage = JsonConvert.DeserializeObject<ConnectionMessage>(e.Message);
                    _keys.Salts = connectionMessage.KeySalts;
                    _encryptedValidationMessage = connectionMessage.EncryptedValidationMessage.Data;

                    await SetKeys(seedString);
                    break;
                case "ping":
                    //this.heartbeat()
                    Instance4Net.Send(Newtonsoft.Json.JsonConvert.SerializeObject("Pong"));
                    break;
                case "ApplyTransactions":
                    break;
                case "SignOut":
                case "UpdateUser":
                case "DeleteUser":
                case "CreateDatabase":
                case "GetDatabase":
                case "OpenDatabase":
                case "Insert":
                case "Update":
                case "Delete":
                case "BatchTransaction":
                case "Bundle":
                case "ValidateKey":
                case "GetPasswordSalts":
                    break;
                default:
                    Console.WriteLine($"Received unknown message from backend: {msg}");
                    break;
            }
        }

        private async Task OnClosed(object sender, EventArgs args, SignInSession session, string seedString, string rememberMe, string username, int reconnectDelay)
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
                    await Reconnect(session, seedString, rememberMe, delay);
                } 
                else if (e.Code == 3001 /*statusCodes['Client Already Connected']*/) {
                    throw new WebSocketError(WsAlreadyConnected, username);
                } 
                else
                {
                    Init(seedString);
                }
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        private async Task SetKeys(string seedString)
        {
            if (_keys.Init) return;

            if (string.IsNullOrEmpty(seedString)) throw new WebSocketError("Missing seed", Session.Username);
            if (_keys.Salts == null) throw new WebSocketError("Missing salts", Session.Username);
            if (string.IsNullOrEmpty(_seedString)) _seedString = seedString;

            var seed = Convert.FromBase64String(seedString);
            _keys.EncryptionKey = AesGcmUtils.ImportKeyFromMaster(seed, Convert.FromBase64String(_keys.Salts.EncryptionKeySalt));
            _keys.DhPrivateKey = DiffieHellmanUtils.ImportKeyFromMaster(seed, Convert.FromBase64String(_keys.Salts.DhKeySalt));
            _keys.HmacKey = HmacUtils.ImportKeyFromMaster(seed, Convert.FromBase64String(_keys.Salts.HmacKeySalt));

            await ValidateKey();

        }

        private async Task ValidateKey()
        {
            var sharedKey = DiffieHellmanUtils.GetSharedKeyWithServer(_keys.DhPrivateKey, await GetServerPublicKey());

        }

        private static byte[] _serverPublicKey;
        private async Task<byte[]> GetServerPublicKey()
        {
            if (_serverPublicKey != null)
                return _serverPublicKey;

            var response = await _api.GetServerPublicKey();
            if (response.IsSuccessStatusCode)
            {
                _serverPublicKey = await response.Content.ReadAsByteArrayAsync();
                return _serverPublicKey;
            }

            var one = await AuthMain.ParseGenericErrors(response);
            if (one != null)
                throw one;

            throw new Exception($"Unknown error during SignIn: {response.StatusCode}");
        }
    }
    
    // TODO: this is temporary only
    public class Session
    {
        public string Username { get; set; }
    }

    public class ConnectionMessage
    {
        public string Route;
        public KeySalts KeySalts;
        public EncryptedValidationMessage EncryptedValidationMessage;
    }

    public class KeySalts
    {
        public string EncryptionKeySalt;
        public string DhKeySalt;
        public string HmacKeySalt;
    }

    public class EncryptedValidationMessage
    {
        public string Type;
        public byte[] Data;
    }

    public class Keys
    {
        public bool Init = false;
        public KeySalts Salts;
        public byte[] EncryptionKey;
        public byte[] DhPrivateKey;
        public byte[] HmacKey;
    }
}
