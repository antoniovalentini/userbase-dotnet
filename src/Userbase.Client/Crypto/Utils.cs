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

        public static byte[] GenerateRandom(int byteSize)
        {
            var result = new byte[byteSize];
            using var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(result);
            return result;
        }

        private const int ONE_KB = 1024;
        private const int TEN_KB = 10 * ONE_KB;
        // https://stackoverflow.com/a/20604561/11601853
        public static string ArrayBufferToString(byte[] buf)
        {
            throw new System.NotImplementedException();
            var bufView = GetShortArray(buf);
            var length = bufView.Length;
            var result = "";
            var chunkSize = TEN_KB; // using chunks prevents stack from blowing up

            for (var i = 0; i < length; i += chunkSize) {
                if (i + chunkSize > length)
                {
                    chunkSize = length - i;
                }

                var chunk = bufView.SubArray(i, chunkSize);
                //result += String.fromCharCode.apply(null, chunk);
            }

            return result;
        }

        private static short[] GetShortArray(byte[] pBaits)
        {
            int length = pBaits.Length / 2 + pBaits.Length % 2;
            short[] ret = new short[length];
            int j = 0;
            for (int i = 0; i < pBaits.Length; i += 2)
            {
                ret[j] = System.BitConverter.ToInt16(pBaits, i);
                j++;
            }
            return (ret);
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            System.Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
