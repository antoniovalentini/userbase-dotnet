using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Userbase.Client.Crypto;
using Userbase.Client.Data.Errors;
using Userbase.Client.Data.Models;
using Userbase.Client.Errors;
using Userbase.Client.Ws;
using Userbase.Client.Ws.Models;
using Timeout = Userbase.Client.Errors.Timeout;

namespace Userbase.Client.Data
{
    public class Db
    {
        private readonly WsWrapper _ws;
        private const int MAX_DB_NAME_CHAR_LENGTH = 50;

        public Db(WsWrapper ws)
        {
            _ws = ws;
        }

        private void ValidateDbInput(OpenDatabaseRequest request)
        {
            var dbName = request.DatabaseName;
            if (string.IsNullOrEmpty(dbName)) throw new DatabaseNameCannotBeBlank();
            if (dbName.Length > MAX_DB_NAME_CHAR_LENGTH) throw new DatabaseNameTooLong(MAX_DB_NAME_CHAR_LENGTH);

            if (_ws.Reconnecting) throw new Reconnecting();
            if (!_ws.Keys.Init) throw new UserNotSignedIn();
        }

        public async Task OpenDatabase(OpenDatabaseRequest request)
        {
            try
            {
                ValidateDbInput(request);
                var ( databaseName, changeHandler ) = request;

                if (!_ws.State.DbNameToHash.TryGetValue(databaseName, out var dbNameHash))
                {
                    dbNameHash = HmacUtils.SignString(_ws.Keys.HmacKey, databaseName);
                    _ws.State.DbNameToHash[databaseName] = dbNameHash;
                }

                var newDatabaseParams = CreateDatabase(databaseName);
                await OpenDatabaseInternal(dbNameHash, changeHandler, newDatabaseParams);
            } catch (Exception ex) {

                if (!(ex is IError e)) throw new UnknownServiceUnavailable(ex);
                switch (e.Name) {
                    case "ParamsMustBeObject":
                    case "DatabaseAlreadyOpening":
                    case "DatabaseNameMustBeString":
                    case "DatabaseNameMissing":
                    case "DatabaseNameCannotBeBlank":
                    case "DatabaseNameTooLong":
                    case "ChangeHandlerMissing":
                    case "ChangeHandlerMustBeFunction":
                    case "UserNotSignedIn":
                    case "UserNotFound":
                    case "TooManyRequests":
                    case "ServiceUnavailable":
                        throw;

                    default:
                        throw new UnknownServiceUnavailable(ex);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="timeout">Timeout in ms</param>
        private static void SetTimeout(Action callback, int timeout)
        {
            var sw = Stopwatch.StartNew();
            while(true)
            {
                if (sw.ElapsedMilliseconds <= timeout) continue;
                callback?.Invoke();
                break;
            }
        }

        private async Task OpenDatabaseInternal(string dbNameHash, Action<List<Database.Item>> changeHandler, (Guid dbId, string encryptedDbKey, string encryptedDbName) newDatabaseParams)
        {
            try
            {
                var firstMessageFromWebSocket = new TaskCompletionSource<int>();

                void ReceivedMessage()
                {
                    firstMessageFromWebSocket.SetResult(0);
                }

                SetTimeout(() => firstMessageFromWebSocket.SetException(new DatabaseAlreadyOpening()), 20000);
                
                /*const firstMessageFromWebSocket = new Promise((resolve, reject) =>
                {
                    receivedMessage = resolve;
                    // TODO: reject after 20s
                    // setTimeout(() => reject(new Error('timeout')), 20000);
                });*/

                if(!_ws.State.Databases.TryGetValue(dbNameHash, out var database))
                {
                    _ws.State.Databases[dbNameHash] = new Database(changeHandler, ReceivedMessage);
                }
                else
                {
                    // safe to replace -- enables idempotent calls to openDatabase
                    database.OnChange = changeHandler;

                    // if 1 call succeeds, all idempotent calls succeed
                    var currentReceivedMessage = new Action(database.ReceivedMessage);
                    database.ReceivedMessage = () =>
                    {
                        currentReceivedMessage();
                        ReceivedMessage();
                    };

                    // database is already open, can return successfully
                    if (database.Init)
                    {
                        changeHandler(database.GetItems());
                        database.ReceivedMessage();
                        return;
                    }
                }

                const string action = "OpenDatabase";
                var paramss = new RequestParams {DbNameHash = dbNameHash, NewDatabaseParams = newDatabaseParams};

                try
                {
                    await _ws.Request(action, paramss);
                    await firstMessageFromWebSocket.Task;
                } catch (WebException e) {
                    if (e.Response == null) throw;

                    await using var stream = e.Response.GetResponseStream();
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    var responseString = reader.ReadToEnd();
                    if (responseString.Equals("Database already creating", StringComparison.InvariantCultureIgnoreCase))
                        throw new DatabaseAlreadyOpening();

                    throw;
                }

            } catch (Exception e)
            {
                var one = ParseGenericErrors(e.Message);
                if (one != null) throw one;
                throw;
            }
        }

        private (Guid dbId, string encryptedDbKey, string encryptedDbName) CreateDatabase(string dbName)
        {
            var dbId = Guid.NewGuid();

            var dbKey = AesGcmUtils.GenerateKey();
            var dbKeyString = AesGcmUtils.GetKeyStringFromKey(dbKey);

            var encryptedDbKey = AesGcmUtils.EncryptString(_ws.Keys.EncryptionKey, dbKeyString);
            var encryptedDbName = AesGcmUtils.EncryptString(dbKey, dbName);

            return (dbId, encryptedDbKey, encryptedDbName);
        }

        public static Exception ParseGenericErrors(string data, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            if (data == "UserNotFound")
                return new UserNotFound();
            if (statusCode == HttpStatusCode.InternalServerError)
                return new InternalServerError();
            if (statusCode == HttpStatusCode.GatewayTimeout)
                return new Timeout();
            return null;
        }
    }
}
