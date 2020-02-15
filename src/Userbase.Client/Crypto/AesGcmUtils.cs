using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Userbase.Client.Models;

namespace Userbase.Client.Crypto
{
    public class AesGcmUtils
    {
        public static string GetSeedStringFromPasswordBasedBackup(byte[] passwordKeyHash, SignInPasswordBasedBackup passwordBasedBackup)
        {
            const int RECOMMENDED_IV_BYTE_SIZE = 12;
            const int RECOMMENDED_AUTHENTICATION_TAG_LENGTH = 16;
            var salt = Convert.FromBase64String(passwordBasedBackup.PasswordBasedEncryptionKeySalt);
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes("password-based-encryption"));
            var passwordBasedEncryptionKey = new Hkdf().DeriveKey(salt, passwordKeyHash, info, 32);

            var aesgcm = new AesGcm(passwordBasedEncryptionKey);
            var ciphertext = Convert.FromBase64String(passwordBasedBackup.PasswordEncryptedSeed);
            var ivStartIndex = ciphertext.Length - RECOMMENDED_IV_BYTE_SIZE;
            var atStartIndex = ciphertext.Length - RECOMMENDED_IV_BYTE_SIZE - RECOMMENDED_AUTHENTICATION_TAG_LENGTH;
            var ciphertextArrayBuffer = ciphertext.Take(atStartIndex).ToArray();
            var iv = new byte[RECOMMENDED_IV_BYTE_SIZE];
            var at = new byte[RECOMMENDED_AUTHENTICATION_TAG_LENGTH];
            Array.Copy(ciphertext, ivStartIndex, iv, 0, RECOMMENDED_IV_BYTE_SIZE);
            Array.Copy(ciphertext, atStartIndex, at, 0, RECOMMENDED_AUTHENTICATION_TAG_LENGTH);

            var plaintextBuffer = new byte[ciphertextArrayBuffer.Length];
            aesgcm.Decrypt(iv, ciphertextArrayBuffer, at, plaintextBuffer);

            var seedStringFromBackup = Convert.ToBase64String(plaintextBuffer);

            return seedStringFromBackup;
        }
    }
}
