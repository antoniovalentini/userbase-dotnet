namespace Userbase.Client.Models
{
    public class SignInResponse
    {
        public string Username { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Profile { get; set; }
        public string InternalProfile { get; set; }
        public bool UsedTempPassword { get; set; }
    }

    public class SignInDto
    {
        public SignInSession Session { get; set; }
        public SignInPasswordBasedBackup PasswordBasedBackup { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Profile { get; set; }
        public string InternalProfile { get; set; }
    }
}