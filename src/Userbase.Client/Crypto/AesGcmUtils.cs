using System;
using System.Security.Cryptography;
using System.Text;
using Userbase.Client.Models;

namespace Userbase.Client.Crypto
{
    public class AesGcmUtils
    {
        public static string GetSeedStringFromPasswordBasedBackup(byte[] passwordKeyHash, SignInPasswordBasedBackup passwordBasedBackup)
        {
            byte[] ciphertext = null;
            byte[] tag = null;
            var plaintext = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes("password-based-encryption"));

            var passwordBasedEncryptionKeySalt = Convert.FromBase64String(passwordBasedBackup.PasswordBasedEncryptionKeySalt);
            var aesgcm = new AesGcm(passwordBasedEncryptionKeySalt);
            aesgcm.Encrypt(passwordKeyHash, plaintext, ciphertext, tag);

            var seedStringFromBackup = Convert.ToBase64String(ciphertext);

            return seedStringFromBackup;
        }
    }
}
