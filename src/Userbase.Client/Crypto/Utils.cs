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
    }
}
