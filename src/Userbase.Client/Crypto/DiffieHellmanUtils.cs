using System.Text;
using System.Threading.Tasks;

namespace Userbase.Client.Crypto
{
    public class DiffieHellmanUtils
    {
        private const string DiffieHellmanKeyName = "diffie-hellman";
        public static byte[] ImportKeyFromMaster(byte[] masterKey, byte[] dhKeySalt)
        {
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(DiffieHellmanKeyName));
            return new Hkdf().DeriveKey(dhKeySalt, masterKey, info, 32);
        }

        public static byte[] GetSharedKeyWithServer(byte[] privateKey, byte[] serverPublicKey)
        {
            //const diffieHellman = createDiffieHellman(privateKey)
            //const sharedSecret = diffieHellman.computeSecret(otherPublicKey)

            //const sharedRawKey = await sha256.hash(sharedSecret)
            //const sharedKey = await aesGcm.getKeyFromRawKey(sharedRawKey)
            //return sharedKey;
            return null;
        }
    }
}
