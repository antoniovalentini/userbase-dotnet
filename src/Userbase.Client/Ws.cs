using System.Net.Http;
using System.Threading.Tasks;
using Userbase.Client.Models;

namespace Userbase.Client
{
    public static class Ws
    {
        public static Session Session { get; set; }

        public static async Task SignOut()
        {
            // TODO
            await Task.FromResult(true);
        }

        public static async Task<HttpResponseMessage> Connect(SignInSession session, string seed, string rememberMe)
        {
            // TODO
            throw new System.NotImplementedException();
        }
    }

    public class Session
    {
        public string Username { get; set; }
    }
}
