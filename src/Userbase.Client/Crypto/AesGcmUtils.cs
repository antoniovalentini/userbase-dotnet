using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Userbase.Client.Models;
// ReSharper disable InconsistentNaming

namespace Userbase.Client.Crypto
{
    public class AesGcmUtils
    {
        private const int RECOMMENDED_IV_BYTE_SIZE = 12;
        private const int RECOMMENDED_AUTHENTICATION_TAG_LENGTH = 16;

        public static string GetSeedStringFromPasswordBasedBackup(byte[] passwordKeyHash, SignInPasswordBasedBackup passwordBasedBackup)
        {
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

        private const string EncryptionKeyName = "encryption";

        public static byte[] ImportKeyFromMaster(byte[] masterKey, byte[] encryptionKeySalt)
        {
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(EncryptionKeyName));
            return new Hkdf().DeriveKey(encryptionKeySalt, masterKey, info, 32);
        }

        public static byte[] Decrypt(byte[] sharedKey, byte[] encryptedValidationMessage)
        {
            var iv = new byte[RECOMMENDED_IV_BYTE_SIZE];
            var at = new byte[RECOMMENDED_AUTHENTICATION_TAG_LENGTH];
            var ivStartIndex = encryptedValidationMessage.Length - RECOMMENDED_IV_BYTE_SIZE;
            var atStartIndex = encryptedValidationMessage.Length - RECOMMENDED_IV_BYTE_SIZE - RECOMMENDED_AUTHENTICATION_TAG_LENGTH;
            Array.Copy(encryptedValidationMessage, ivStartIndex, iv, 0, RECOMMENDED_IV_BYTE_SIZE);
            Array.Copy(encryptedValidationMessage, atStartIndex, at, 0, RECOMMENDED_AUTHENTICATION_TAG_LENGTH);
            var ciphertextArrayBuffer = encryptedValidationMessage.Take(atStartIndex).ToArray();
            var plaintextBuffer = new byte[ciphertextArrayBuffer.Length];

            using var aesGcm = new AesGcm(sharedKey);
            aesGcm.Decrypt(iv, ciphertextArrayBuffer, at, plaintextBuffer);
            return plaintextBuffer;
        }

        private const string PASSWORD_BASED_ENCRYPTION_KEY = "password-based-encryption";
        public static byte[] GetPasswordBasedEncryptionKey(string passwordHash, byte[] passwordBasedEncryptionKeySalt)
        {
            var ikm = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(passwordHash));
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(PASSWORD_BASED_ENCRYPTION_KEY));
            return new Hkdf().DeriveKey(passwordBasedEncryptionKeySalt, ikm, info, 32);
        }

        public static byte[] Encrypt(byte[] encryptionKey, byte[] seed)
        {
            var iv = Utils.GenerateRandom(RECOMMENDED_IV_BYTE_SIZE);
            var at = new byte[RECOMMENDED_AUTHENTICATION_TAG_LENGTH];
            var ciphertext = new byte[seed.Length];
            using var aesGcm = new AesGcm(encryptionKey);
            aesGcm.Encrypt(iv, seed, ciphertext, at);

            var result = new byte[ciphertext.Length + at.Length + iv.Length];
            Array.Copy(ciphertext, result, ciphertext.Length);
            Array.Copy(at, 0, result, ciphertext.Length, RECOMMENDED_AUTHENTICATION_TAG_LENGTH);
            Array.Copy(iv, 0, result, ciphertext.Length + RECOMMENDED_AUTHENTICATION_TAG_LENGTH, RECOMMENDED_IV_BYTE_SIZE);

            return result;
        }

        private const int BIT_SIZE = 256;

        public static byte[] GenerateKey()
        {
            return Utils.GenerateRandom(BIT_SIZE / 8);
        }

        public static string GetKeyStringFromKey(byte[] dbKey)
        {
            //const rawKey = await window.crypto.subtle.exportKey(RAW_KEY_TYPE, key)
            return Convert.ToBase64String(dbKey);
        }

        public static string EncryptString(byte[] key, string plaintextString)
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintextString);
            var plaintextArrayBuffer = Utils.FillOddsWithZeros(plaintextBytes);
            var encrypted = Encrypt(key, plaintextArrayBuffer);
            return Convert.ToBase64String(encrypted);
        }

        public static string DecryptString(byte[] key, string encryptedString)
        {
            var encryptedArrayBuffer = Convert.FromBase64String(encryptedString);
            var plaintextArrayBuffer = Decrypt(key, encryptedArrayBuffer);
            var plaintextString = Utils.ArrayBufferToString(plaintextArrayBuffer);
            return plaintextString;
        }
    }
}
