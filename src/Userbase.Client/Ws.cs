using System.Threading.Tasks;

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
    }

    public class Session
    {
        public string Username { get; set; }
    }
}
