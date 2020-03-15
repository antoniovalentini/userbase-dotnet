using System;
using Newtonsoft.Json;

namespace Userbase.Client.Ws.Models
{
    public class RequestParams
    {
        [JsonProperty("validationMessage")]
        public string ValidationMessage { get; set; }
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
        [JsonProperty("dbNameHash")]
        public string DbNameHash { get; set; }
        [JsonProperty("newDatabaseParams")]
        public DatabaseParams NewDatabaseParams { get; set; }
    }

    public class DatabaseParams
    {
        [JsonProperty("dbId")]
        public Guid DbId;
        [JsonProperty("encryptedDbKey")]
        public string EncryptedDbKey;
        [JsonProperty("encryptedDbName")]
        public string EncryptedDbName;

        public DatabaseParams((Guid dbId, string encryptedDbKey, string encryptedDbName) newDatabaseParams)
        {
            var (dbId, encryptedDbKey, encryptedDbName) = newDatabaseParams;
            DbId = dbId;
            EncryptedDbKey = encryptedDbKey;
            EncryptedDbName = encryptedDbName;
        }
    }
}
