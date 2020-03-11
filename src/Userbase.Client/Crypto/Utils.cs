using System.Security.Cryptography;

namespace Userbase.Client.Crypto
{
    public static class Utils
    {
        // TODO: investigate why they double the size when converting to bytes
        public static byte[] FillOddsWithZeros(byte[] source)
        {
            var dest = new byte[source.Length*2];
            var j = 0;
            foreach (var t in source)
            {
                dest[j] = t;
                j+=2;
            }
            return dest;
        }

        private static int SEED_BYTE_SIZE = 32; // 256 / 8
        public static byte[] GenerateSeed()
        {
            var result = new byte[SEED_BYTE_SIZE];
            using var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(result);
            return result;
        }

        public static byte[] GenerateRandom(int size)
        {
            var result = new byte[size];
            using var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(result);
            return result;
        }
    }
}
