namespace Userbase.Client.Models
{
    public class GetPasswordSaltsResponse
    {
        public string PasswordSalt { get; set; }
        public string PasswordTokenSalt { get; set; }
    }
}