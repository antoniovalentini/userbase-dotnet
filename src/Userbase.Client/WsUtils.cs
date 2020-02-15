namespace Userbase.Client
{
    public static class WsUtils
    {
        //TODO: improve using URI methods
        private static string RemoveProtocolFromEndpoint (string endpoint) {
            const string http = "http://";
            const string https = "https://";

            if (endpoint.Substring(0, http.Length) == http)
                return endpoint.Substring(http.Length);

            if (endpoint.Substring(0, https.Length) == https)
                return endpoint.Substring(https.Length);

            return endpoint;
        }

        private static string GetProtocolFromEndpoint(string endpoint)
        {
            return endpoint.Split(':')[0];
        }

        public static string GetWsUrl (string endpoint)  {
            var host = RemoveProtocolFromEndpoint(endpoint);
            var protocol = GetProtocolFromEndpoint(endpoint);

            return (protocol == "https" ? "wss://" : "ws://") + host;
        }
    }
}
