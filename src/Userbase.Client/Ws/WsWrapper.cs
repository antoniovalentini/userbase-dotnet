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
using Userbase.Client.Data;
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

        public WebSocket Instance4Net { get; private set; }

        private readonly Config _config;
        private readonly AuthApi _api;
        private readonly ILogger _logger;
        private readonly LocalData _localData;
        private byte[] _encryptedValidationMessage;
        private string _seedString;
        private bool _connectionResolved;
        private Action _resolveConnection;
        private Action<IError> _rejectConnection;

        private const string WsAlreadyConnected = "Web Socket already connected";

        private bool _connected;
        public bool Reconnecting { get; private set; }
        private bool _reconnected;
        public SignInSession Session { get; } = new SignInSession();
        private string _rememberMe;
        public UserState State { get; private set; }
        private int _pingTimeout;
        public Keys Keys { get; } = new Keys();
        private readonly string _clientId;
        private DiffieHellmanUtils _dh;
        private readonly Dictionary<string, WsRequest> _pendingRequests;

        public WsWrapper(Config config, AuthApi api, ILogger logger, LocalData localData)
        {
            _config = config;
            _api = api;
            _logger = logger;
            _localData = localData;
            _clientId = Guid.NewGuid().ToString();
            _pendingRequests = new Dictionary<string, WsRequest>();
        }

        public void Init(Action resolveConnection = null, Action<IError> rejectConnection = null, SignInSession session = null, string seedString = null, string rememberMe = null, UserState state = null)
        {
            if (_pingTimeout > 0) ClearTimeout(_pingTimeout);

            // TODO: find out the scope of this code
            //for (const property of Object.keys(this)) {
            //    delete this[property];
            //}

            if (Instance4Net != null)
            {
                Instance4Net.Dispose();
                Instance4Net = null;
            }

            _connected = false;

            _resolveConnection = resolveConnection;
            _rejectConnection = rejectConnection;
            _connectionResolved = false;

            Session.Username = session != null && !string.IsNullOrEmpty(session.Username) ? session.Username : "";
            Session.SessionId = session != null && !string.IsNullOrEmpty(session.SessionId) ? session.SessionId : "";
            Session.CreationDate = session != null && !string.IsNullOrEmpty(session.CreationDate) ? session.CreationDate : "";

            _seedString = seedString;
            Keys.Clear();

            _rememberMe = rememberMe;

            _pendingRequests.Clear();

            State = state ?? new UserState
            {
                Databases = new Dictionary<string, Database>(),
                dbIdToHash = null,
                DbNameToHash = new Dictionary<string, string>(),
            };
        }

        private void ClearTimeout(int pingTimeout)
        {
            // TODO
        }

        public void Close(string code = "")
        {
            if (Instance4Net != null)
                Instance4Net.Close(code);
            else 
                Init();
        }

        public async Task SignOut()
        {
            var username = Session.Username;
            var connectionResolved = _connectionResolved;
            var rejectConnection = _rejectConnection;

            try
            {
                _localData.SignOutSession(_rememberMe, username);

                var sessionId = Session.SessionId;

                if (Reconnecting) throw new Reconnecting();

                const string action = "SignOut";
                var reqParams = new RequestParams {SessionId = sessionId};
                await Request(action, reqParams);

                Close();

                if (!connectionResolved)
                    rejectConnection?.Invoke(new WebSocketError("Canceled", username));

            } catch {
                if (!connectionResolved)
                    rejectConnection?.Invoke(new WebSocketError("Canceled", username));

                throw;
            }
        }

        // TODO: handle state
        public async Task Reconnect(SignInSession session, string seed, string rememberMe, UserState state, int reconnectDelay = 0)
        {
            await Task.FromException(new NotImplementedException());
        }

        public async Task<HttpResponseMessage> Connect(SignInSession session, string seedString, string rememberMe, int reconnectDelay = 0)
        {
            if (_connected) throw new WebSocketError(WsAlreadyConnected, session.Username);
            
            var timeout = false;
            var timeoutToOpenWebSocket = SetTimeout(
                () =>
                {
                    if (!_connected && !Reconnecting)
                    {
                        timeout = true;
                        throw new WebSocketError("timeout", "");
                    }
                },
                10000
            );

            var url = $"{WsUtils.GetWsUrl(_config.Endpoint)}api?appId={_config.AppId}&sessionId={session.SessionId}&clientId={_clientId}";

            // TODO: handle timeouts
            var webSocket = new WebSocket(url);
            webSocket.Opened += (sender, e) => OnOpened(sender, e, timeout, timeoutToOpenWebSocket);
            webSocket.DataReceived += OnDataReceived;
            webSocket.MessageReceived += async (sender, args) => await OnMessageReceived(sender, args, webSocket, seedString, session);
            webSocket.Closed += async (sender, args) => await OnClosed(sender, args, session, seedString, rememberMe, reconnectDelay, timeout);
            webSocket.Error += OnError;
            var result = await webSocket.OpenAsync();

            return result
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        private int SetTimeout(Action action, int i)
        {
            // TODO
            return 0;
        }

        private void OnOpened(object sender, EventArgs e, bool timeout, int timeoutToOpenWebSocket)
        {
            if (timeout) return;
            ClearTimeout(timeoutToOpenWebSocket);
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Hello!");
        }

        private async Task OnMessageReceived(object sender, MessageReceivedEventArgs e, WebSocket webSocket, string seedString, SignInSession session)
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
                        // TODO: pass parameters
                        Init(null, null, session, seedString, null, null);
                        Instance4Net = webSocket;
                        //this.heartbeat()
                        _connected = true;

                        var connectionMessage = JsonConvert.DeserializeObject<ConnectionMessage>(e.Message);
                        Keys.Salts = connectionMessage.KeySalts;
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

        private async Task OnClosed(object sender, EventArgs args, SignInSession session, string seedString, string rememberMe, int reconnectDelay, bool timeout)
        {
            _logger.Log("CLOSED");
            if (timeout) return;
            if (args is ClosedEventArgs e)
            {
                // TODO
                var serviceRestart = e.Code == 1012; // statusCodes['Service Restart']
                var clientDisconnected = e.Code == 3000; //statusCodes['No Pong Received']
                var attemptToReconnect =
                    serviceRestart ||
                    clientDisconnected; // || !e.wasClean // closed without explicit call to ws.close()

                if (attemptToReconnect)
                {
                    var delay = serviceRestart && reconnectDelay <= 0
                        ? 0
                        : (reconnectDelay > 0 ? reconnectDelay + BackoffRetryDelay : 1000);

                    Reconnecting = true;
                    await Reconnect(session, seedString, rememberMe, !_reconnected && State != null ? State : null, delay);
                }
                else if (e.Code == 3001 /*statusCodes['Client Already Connected']*/)
                {
                    // TODO: reject websocketError?
                    throw new WebSocketError(WsAlreadyConnected, session.Username);
                }
                else
                {
                    Init(null, null, null, null, null, null);
                }
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        private async Task SetKeys(string seedString)
        {
            if (Keys.Init) return;

            if (string.IsNullOrEmpty(seedString)) throw new WebSocketError("Missing seed", Session.Username);
            if (Keys.Salts == null) throw new WebSocketError("Missing salts", Session.Username);
            if (string.IsNullOrEmpty(_seedString)) _seedString = seedString;

            var seed = Convert.FromBase64String(seedString);
            Keys.EncryptionKey = AesGcmUtils.ImportKeyFromMaster(seed, Convert.FromBase64String(Keys.Salts.EncryptionKeySalt));
            Keys.DhPrivateKey = DiffieHellmanUtils.ImportKeyFromMaster(seed, Convert.FromBase64String(Keys.Salts.DhKeySalt));
            Keys.HmacKey = HmacUtils.ImportKeyFromMaster(seed, Convert.FromBase64String(Keys.Salts.HmacKeySalt));

            await ValidateKey();

            Keys.Init = true;

            _resolveConnection?.Invoke();

            _connectionResolved = true;
        }

        private async Task ValidateKey()
        {
            if (_dh == null)
                _dh = new DiffieHellmanUtils(await GetServerPublicKey());

            var sharedKey = _dh.GetSharedKeyWithServer(Keys.DhPrivateKey);
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

        public async Task Request(string action, RequestParams reqParams)
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

            var one = AuthMain.ParseGenericErrors(await response.Content.ReadAsStringAsync(), response.StatusCode);
            if (one != null)
                throw one;

            throw new Exception($"Unknown error during SignIn: {response.StatusCode}");
        }
    }
}
