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
    }
}
