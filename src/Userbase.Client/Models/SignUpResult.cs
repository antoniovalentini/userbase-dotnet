using System.Collections.Generic;

namespace Userbase.Client.Models
{
    public class SignUpResult
    {
        public string Username;
        public string UserId;
        public string Email;
        public Dictionary<string, string> Profile;
    }
}
