namespace Userbase.Client.Models
{
    public class SignInPasswordBasedBackup
    {
        public string PasswordBasedEncryptionKeySalt { get; set; }
        public string PasswordEncryptedSeed { get; set; }
    }
}