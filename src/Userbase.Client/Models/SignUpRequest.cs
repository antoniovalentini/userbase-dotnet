using System.Collections.Generic;

namespace Userbase.Client.Models
{
    public class SignUpRequest
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string RememberMe { get; set; }
        public Dictionary<string, string> Profile { get; set; }
    }
}
