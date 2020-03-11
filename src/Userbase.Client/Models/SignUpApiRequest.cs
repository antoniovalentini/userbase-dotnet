using System.Collections.Generic;

namespace Userbase.Client.Models
{
    public class SignUpApiRequest
    {
        public string Username;
        public string PasswordToken;
        public byte[] PublicKey;
        public dynamic PasswordSalts;
        public dynamic KeySalts;
        public string Email;
        public Dictionary<string, string> Profile;
        public dynamic PasswordBasedBackup;
    }
}
