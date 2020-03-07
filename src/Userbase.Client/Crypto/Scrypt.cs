using System;
using System.Security.Cryptography;
using System.Text;

namespace Userbase.Client.Crypto
{
    public static class Scrypt
    {
        const int n = 16384;
        const int r = 8;
        const int p = 1;
        const int hashLength = 32;

        public static string Hash(string password, string passwordSalt)
        {
            var salt = Convert.FromBase64String(passwordSalt);
            return Hash(password, salt);
        }

        public static string Hash(string password, byte[] passwordSalt)
        {
            var key = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(password));
            var hash = CryptSharp.Utility.SCrypt.ComputeDerivedKey(key, passwordSalt, n, r, p, null, hashLength);
            return Convert.ToBase64String(hash);
        }

        /**
         * NIST recommendation:
         *
         * "The length of the randomly-generated portion of the salt shall be at least 128 bits."
         *
         * Section 5.1
         * https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-132.pdf
         *
         **/
        private const int SALT_LENGTH = 16;
        public static byte[] GenerateSalt() 
        {
            var result = new byte[SALT_LENGTH];
            using var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(result);
            return result;
        }
    }
}
