using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Userbase.Client.Api
{
    public class AuthApi
    {
        private readonly Config _config;
        private readonly HttpClient _client;

        public AuthApi(Config config)
        {
            _config = config;
            _client = new HttpClient {BaseAddress = new Uri(_config.Endpoint)};
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add("User-Agent", ".NET");
        }

        public async Task<HttpResponseMessage> SignIn(string username, string passwordToken)
        {
            var data = JsonConvert.SerializeObject(new { username , passwordToken });
            var content = new StringContent(data, Encoding.UTF8, "application/json");
            return await _client.PostAsync($"api/auth/sign-in?appId={_config.AppId}", content);
        }

        public async Task<HttpResponseMessage> GetPasswordSalts(string username)
        {
            return await _client.GetAsync($"api/auth/get-password-salts?appId={_config.AppId}&username={username}");
        }

        public async Task<HttpResponseMessage> GetServerPublicKey()
        {
            return await _client.GetAsync("api/auth/server-public-key");
        }
    }
}
