namespace Userbase.Client.Models
{
    public class RebuildPasswordTokenResponse
    {
        public string PasswordToken { get; set; }
        public byte[] PasswordHkdfKey { get; set; }
    }
}
