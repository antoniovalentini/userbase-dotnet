using System;
using System.Collections.Generic;

namespace Userbase.Client
{
    public class Config
    {
        public string AppId { get; }
        // Always put a slash at the end of a HttpClient BaseAddress
        // Never put a starting slash on the requestUri
        // https://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working
        public string Version => "v1/";
        public string Endpoint => "https://v1.userbase.com/" + Version;

        public static readonly Dictionary<string, bool> RememberMeOptions = new Dictionary<string, bool>
        {
            {"local", true},
            {"session", true},
            {"none", true }
        };

        public Config(string appId)
        {
            AppId = appId ?? throw new ArgumentNullException(nameof(appId));
        }
    }
}
