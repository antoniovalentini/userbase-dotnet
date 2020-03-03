namespace Userbase.Client.Ws.Models
{
    // TODO: this is temporary only
    public class UserSession
    {
        public string Username;
        public string SessionId;
        public string CreationDate;

        public void Clear()
        {
            Username = string.Empty;
            SessionId = string.Empty;
            CreationDate = string.Empty;
        }
    }
}
