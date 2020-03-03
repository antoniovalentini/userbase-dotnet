using Newtonsoft.Json;

namespace Userbase.Client.Ws.Models
{
    public class RequestParams
    {
        [JsonProperty("validationMessage")]
        public string ValidationMessage { get; set; }
    }
}
