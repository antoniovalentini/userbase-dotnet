using System;
using System.Security.Cryptography;
using System.Text;

namespace Userbase.Client.Crypto
{
    public class AesGcmUtils
    {
        public string GetSeedStringFromPasswordBasedBackup(byte[] passwordKeyHash, byte[] passwordBasedEncryptionKeySalt)
        {
            byte[] ciphertext = null;
            byte[] tag = null;
            var plaintext = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes("password-based-encryption"));

            var aesgcm = new AesGcm(passwordBasedEncryptionKeySalt);
            aesgcm.Encrypt(passwordKeyHash, plaintext, ciphertext, tag);

            var seedStringFromBackup = Convert.ToBase64String(ciphertext);

            return seedStringFromBackup;
        }
    }
}
