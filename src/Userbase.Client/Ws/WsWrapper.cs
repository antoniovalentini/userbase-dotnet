using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;
using Userbase.Client.Api;
using Userbase.Client.Crypto;
using Userbase.Client.Errors;
using Userbase.Client.Models;
using Userbase.Client.Ws.Models;
using Userbase.Client.Ws.Errors;
using WebSocket4Net;

namespace Userbase.Client.Ws
{
    public class WsWrapper
    {
        private const int BackoffRetryDelay = 1000;

        public static WebSocket Instance4Net;

        private readonly Config _config;
        private readonly AuthApi _api;
        private readonly ILogger _logger;
        private readonly Keys _keys = new Keys();
        private byte[] _encryptedValidationMessage;
        private string _seedString;
        private bool _connectionResolved;
        private Action _resolveConnection;
        private Action _rejectConnection;

        private const string WsAlreadyConnected = "Web Socket already connected";

        public bool Connected { get; set; }
        public bool Reconnecting { get; set; }
        public Session Session { get; set; }
        public string ClientId { get; }
        private DiffieHellmanUtils _dh;
        private readonly Dictionary<string, WsRequest> _pendingRequests;

        public WsWrapper(Config config, AuthApi api, ILogger logger)
        {
            _config = config;
            _api = api;
            _logger = logger;
            ClientId = Guid.NewGuid().ToString();
            _pendingRequests = new Dictionary<string, WsRequest>();
        }

        public void Init(Action resolveConnection, Action rejectConnection, string seedString)
        {
            _resolveConnection = resolveConnection;
            _rejectConnection = rejectConnection;
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
            //_logger.Log(sender.ToString());
            _logger.Log($"RECEIVED - {e.Message}");
            try
            {
                var msg = JObject.Parse(e.Message);
                var route = msg["route"]?.ToString().ToLower() ?? "";

                switch (route)
                {
                    case "connection":
                        // TODO: pass equivalent of promise resolve and reject
                        Init(null, null, seedString);
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
                        _logger.Log("SENT - PONG");
                        const string action = "Pong";
                        Instance4Net.Send(JsonConvert.SerializeObject(new {action}));
                        break;
                    case "applytransactions":
                        break;
                    case "signout":
                    case "updateuser":
                    case "deleteuser":
                    case "createdatabase":
                    case "getdatabase":
                    case "opendatabase":
                    case "insert":
                    case "update":
                    case "delete":
                    case "batchtransaction":
                    case "bundle":
                    case "validatekey":
                    case "getpasswordsalts":
                        var requestId = msg["requestId"]?.ToString().ToLower() ?? "";

                        if (string.IsNullOrEmpty(requestId))
                        {
                            // TODO: log warning
                            Console.WriteLine("Missing request id");
                            return;
                        }

                        //var request = this.requests[requestId];
                        if (!_pendingRequests.TryGetValue(requestId, out var request))
                        {
                            // TODO: log warning
                            Console.WriteLine($"Request {requestId} no longer exists!");
                            return;
                        }
                        else if (request.Resolve == null || request.Reject == null)
                            return;

                        var response = msg["response"]?.ToString().ToLower() ?? "";
                        var status = msg["response"]["status"]?.ToString().ToLower() ?? "";

                        var statusCode = HttpStatusCode.BadRequest;
                        var successfulResponse = !string.IsNullOrEmpty(response)
                                                 && !string.IsNullOrEmpty(status)
                                                 && Enum.TryParse(status, out statusCode)
                                                 && statusCode == HttpStatusCode.OK;

                        if (successfulResponse) request.Resolve(requestId);
                        else request.Reject(requestId, route, statusCode, response);

                        break;
                    default:
                        Console.WriteLine($"Received unknown message from backend: {msg}");
                        break;
                }
            }
            catch (Exception exception)
            {
                _logger.Log($"ERROR - {exception.Message}");
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

                if (attemptToReconnect)
                {
                    var delay = serviceRestart && reconnectDelay <= 0
                        ? 0
                        : (reconnectDelay > 0 ? reconnectDelay + BackoffRetryDelay : 1000);

                    Reconnecting = true;
                    await Reconnect(session, seedString, rememberMe, delay);
                }
                else if (e.Code == 3001 /*statusCodes['Client Already Connected']*/)
                {
                    throw new WebSocketError(WsAlreadyConnected, username);
                }
                else
                {
                    Init(null, null, null);
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

            _keys.Init = true;

            _resolveConnection?.Invoke();

            _connectionResolved = true;
        }

        private async Task ValidateKey()
        {
            if (_dh == null)
                _dh = new DiffieHellmanUtils(await GetServerPublicKey());

            var sharedKey = _dh.GetSharedKeyWithServer(_keys.DhPrivateKey);
            var validationMessage = Convert.ToBase64String(AesGcmUtils.Decrypt(sharedKey, _encryptedValidationMessage));

            await Request("ValidateKey", new RequestParams { ValidationMessage = validationMessage });
        }

        private void OnSuccess(string requestId)
        {
            PendingRequestComplete(requestId);
        }

        private void OnFailure(string requestId, string action, HttpStatusCode statusCode, string response)
        {
            PendingRequestComplete(requestId);
            var msg = JObject.Parse(response);
            var message = msg["message"]?.ToString() ?? "";

            if (statusCode != HttpStatusCode.TooManyRequests)
                throw new RequestFailed(action, statusCode,
                    string.IsNullOrEmpty(message)
                        ? $"Unable to parse message from {JsonConvert.SerializeObject(response)}"
                        : message);

            // retryDelay parsing
            var data = msg["data"]?.ToString() ?? "";
            float retryDelay = -1;
            if (string.IsNullOrEmpty(data))
                throw new TooManyRequests(retryDelay);
            var retryString = msg["message"]["retryDelay"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(retryString))
                throw new TooManyRequests(retryDelay);

            float.TryParse(retryString, out retryDelay);
            throw new TooManyRequests(retryDelay);
        }

        private async Task Request(string action, RequestParams reqParams)
        {
            // generate a new requestId
            var requestId = Guid.NewGuid().ToString();

            // add new pending request with success/failure callback when the WebSocket
            // receives a response for this requestId — the watch
            // would time out of x seconds
            await Watch(requestId, OnSuccess, OnFailure);

            // send the request on the WebSocket
            var message = JsonConvert.SerializeObject(new
            {
                requestId,
                action,
                @params = reqParams,
            });
            Instance4Net.Send(message);
            _logger.Log($"SENT - {message}");

            // wait for the response to arrive
        }

        private async Task Watch(string requestId, Action<string> resolve, Action<string, string, HttpStatusCode, string> reject)
        {
            _pendingRequests.Add(requestId, new WsRequest
            {
                Resolve = resolve,
                Reject = reject,
            });

            // TODO
            // setTimeout(() => { reject(new Error('timeout')) }, 10000)

            await Task.FromResult(0);
        }

        private void PendingRequestComplete(string requestId)
        {
            if (_pendingRequests.ContainsKey(requestId))
                _pendingRequests.Remove(requestId);
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
}
