using System.Collections.Generic;
using Newtonsoft.Json;

namespace Userbase.Client.Models
{
    public class SignUpApiRequest
    {
        [JsonProperty("username")]
        public string Username;
        [JsonProperty("passwordToken")]
        public string PasswordToken;
        [JsonProperty("publicKey")]
        public byte[] PublicKey;
        [JsonProperty("passwordSalts")]
        public PasswordSalts PasswordSalts;
        [JsonProperty("keySalts")]
        public KeySalts KeySalts;
        [JsonProperty("email")]
        public string Email;
        [JsonProperty("profile")]
        public Dictionary<string, string> Profile;
        [JsonProperty("passwordBasedBackup")]
        public PasswordBasedBackup PasswordBasedBackup;
    }

    public class PasswordSalts
    {
        [JsonProperty("passwordSalt")]
        public string PasswordSalt { get; set; }
        [JsonProperty("passwordTokenSalt")]
        public string PasswordTokenSalt { get; set; }
    }

    public class KeySalts
    {
        [JsonProperty("encryptionKeySalt")]
        public string EncryptionKeySalt { get; set; }
        [JsonProperty("dhKeySalt")]
        public string DhKeySalt { get; set; }
        [JsonProperty("hmacKeySalt")]
        public string HmacKeySalt { get; set; }
    }

    public class PasswordBasedBackup
    {
        [JsonProperty("passwordBasedEncryptionKeySalt")]
        public string PasswordBasedEncryptionKeySalt { get; set; }
        [JsonProperty("passwordEncryptedSeed")]
        public string PasswordEncryptedSeed { get; set; }
    }
}
