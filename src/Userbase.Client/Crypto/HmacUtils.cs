using System;
using System.Security.Cryptography;
using System.Text;

namespace Userbase.Client.Crypto
{
    public class HmacUtils
    {
        private const string HmacKeyName = "authentication";
        public static byte[] ImportKeyFromMaster(byte[] masterKey, byte[] dhKeySalt)
        {
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(HmacKeyName));
            return new Hkdf().DeriveKey(dhKeySalt, masterKey, info, 32);
        }

        // TODO: check
        public static string SignString(byte[] key, string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var dataByteWithZeros = Utils.FillOddsWithZeros(dataBytes);
            using var hmac = new HMACSHA256(key);
            var result = hmac.ComputeHash(dataByteWithZeros);
            return Convert.ToBase64String(result);
        }
    }
}
