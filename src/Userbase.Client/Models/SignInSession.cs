namespace Userbase.Client.Models
{
    public class SignInSession
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