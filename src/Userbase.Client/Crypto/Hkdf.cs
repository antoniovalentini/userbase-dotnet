using System;
using System.Security.Cryptography;
using System.Text;

namespace Userbase.Client.Crypto
{
    /// <summary>
    /// Thanks to CodesInChaos for this implementation.
    /// https://gist.github.com/CodesInChaos/8710228
    /// </summary>
    public class Hkdf
    {
        Func<byte[],byte[],byte[]> keyedHash;

        public Hkdf()
        {
            var hmac = new HMACSHA256();
            keyedHash = (key, message)=>
            {
                hmac.Key=key;
                return hmac.ComputeHash(message);
            };
        }

        public byte[] Extract(byte[] salt, byte[] inputKeyMaterial)
        {
            return keyedHash(salt, inputKeyMaterial);
        }

        public byte[] Expand(byte[] prk, byte[] info, int outputLength)
        {
            var resultBlock = new byte[0];
            var result = new byte[outputLength];
            var bytesRemaining = outputLength;
            for (int i = 1; bytesRemaining > 0; i++)
            {
                var currentInfo = new byte[resultBlock.Length + info.Length + 1];
                Array.Copy(resultBlock, 0, currentInfo, 0, resultBlock.Length);
                Array.Copy(info, 0, currentInfo, resultBlock.Length, info.Length);
                currentInfo[currentInfo.Length - 1] = (byte)i;
                resultBlock = keyedHash(prk, currentInfo);
                Array.Copy(resultBlock, 0, result, outputLength - bytesRemaining, Math.Min(resultBlock.Length, bytesRemaining));
                bytesRemaining -= resultBlock.Length;
            }
            return result;
        }

        public byte[] DeriveKey(byte[] salt, byte[] inputKeyMaterial, byte[] info, int outputLength)
        {
            var prk = Extract(salt, inputKeyMaterial);
            var result = Expand(prk, info, outputLength);
            return result;
        }

        /**
        *  RFC 5869:
        *
        *  "the use of salt adds significantly to the strength of HKDF...
        *  Ideally, the salt value is a random (or pseudorandom) string of the
        *  length HashLen"
        *
        *  https://tools.ietf.org/html/rfc5869#section-3.1
        *
        **/
        private const int SALT_BYTE_SIZE = Sha256Custom.BYTE_SIZE;
        public static byte[] GenerateSalt()
        {
            var result = new byte[SALT_BYTE_SIZE];
            using var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(result);
            return result;
        }

        public static object GetPasswordToken(string passwordhash, byte[] salt)
        {
            var ikm = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(passwordhash));
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes("password-token"));
            //var salt = Convert.FromBase64String(salts.PasswordTokenSalt);
            var passwordToken = new Hkdf().DeriveKey(salt, ikm, info, 32);

            return Convert.ToBase64String(passwordToken);
        }
    }
}
