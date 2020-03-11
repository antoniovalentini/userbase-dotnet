namespace Userbase.Client.Models
{
    public class SignInRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string RememberMe { get; set; }
    }
}
