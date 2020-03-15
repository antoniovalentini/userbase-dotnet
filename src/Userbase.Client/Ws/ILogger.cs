using System.Threading.Tasks;

namespace Userbase.Client.Ws
{
    public interface ILogger
    {
        Task Log(string message);
    }
}
