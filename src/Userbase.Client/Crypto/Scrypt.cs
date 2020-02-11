using System;
using System.Text;
using CryptSharp.Utility;

namespace Userbase.Client.Crypto
{
    public static class Scrypt
    {
        public static string Hash(string password, string passwordSalt)
        {
            const int n = 16384;
            const int r = 8;
            const int p = 1;
            const int hashLength = 32;
            
            var key = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(password));
            var salt = Convert.FromBase64String(passwordSalt);
            var hash = SCrypt.ComputeDerivedKey(key, salt, n, r, p, null, hashLength);

            return Convert.ToBase64String(hash);
        }
    }
}
