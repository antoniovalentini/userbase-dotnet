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

        public string DbNameHash { get; set; }
        public (Guid dbId, string encryptedDbKey, string encryptedDbName) NewDatabaseParams { get; set; }
    }
}
