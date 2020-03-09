using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Userbase.Client.Crypto
{
    public class DiffieHellmanUtils
    {
        private readonly byte[] _bobPublicKey;

        // RFC 3526 detailing publicly known 2048 bit safe prime: https://www.ietf.org/rfc/rfc3526.txt
        private static string PrimeString = "ffffffffffffffffc90fdaa22168c234c4c6628b80dc1cd129024e088a67cc74020bbea63b139b22514a08798e3404ddef9519b3cd3a431b302b0a6df25f14374fe1356d6d51c245e485b576625e7ec6f44c42e9a637ed6b0bff5cb6f406b7edee386bfb5a899fa5ae9f24117c4b1fe649286651ece45b3dc2007cb8a163bf0598da48361c55d39a69163fa8fd24cf5f83655d23dca3ad961c62f356208552bb9ed529077096966d670c354e4abc9804f1746c08ca18217c32905e462e36ce3be39e772c180e86039b2783a2ec07a28fb5c55df06f4c52c9de2bcbf6955817183995497cea956ae515d2261898fa051015728e5a8aacaa68ffffffffffffffff"  ;
        private static int[] GENERATOR = {2};
        private const string DiffieHellmanKeyName = "diffie-hellman";

        /// <summary>
        /// Initialize diffie-hellman utilities class
        /// </summary>
        /// <param name="bobPublicKey">Userbase public key</param>
        public DiffieHellmanUtils(byte[] bobPublicKey)
        {
            _bobPublicKey = bobPublicKey;
        }

        public static byte[] ImportKeyFromMaster(byte[] masterKey, byte[] dhKeySalt)
        {
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(DiffieHellmanKeyName));
            return new Hkdf().DeriveKey(dhKeySalt, masterKey, info, 32);
        }

        public byte[] GetSharedKeyWithServer(byte[] privateKey)
        {
            _ = _bobPublicKey ?? throw new NullReferenceException("No public key has been set during initialization.");

            //var g = new BigInteger(2);
            var a = BigInteger.Parse("0" + ByteArrayToString(privateKey), NumberStyles.HexNumber);
            var primeBig = BigInteger.Parse("0" + PrimeString, NumberStyles.HexNumber);
            var bobPublicBig = BigInteger.Parse("0" + ByteArrayToString(_bobPublicKey), NumberStyles.HexNumber);

            var secret = BigInteger.ModPow(bobPublicBig, a, primeBig);
            var binSecret = secret.ToByteArray(true).Reverse().ToArray();
            if (binSecret.Length < primeBig.GetByteCount()) {
                // TODO: still to be tested
                var front = new byte[primeBig.GetByteCount() - binSecret.Length];
                var rv = new byte[front.Length + binSecret.Length];
                Buffer.BlockCopy(front, 0, rv, 0, front.Length);
                Buffer.BlockCopy(binSecret, 0, rv, front.Length, binSecret.Length);
            }

            using var sha256Hash = SHA256.Create();
            return sha256Hash.ComputeHash(binSecret);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        static byte[] HexStringToBytes(string source)
        {
            var totalChars = source.Length;
            var bytes = new byte[totalChars / 2];
            for (var i = 0; i < totalChars; i += 2)
            {
                var substring = source.Substring(i, 2);
                bytes[i / 2] = Convert.ToByte(substring, 16);
            }
            return bytes;
        }

        public static byte[] GetPublicKey(byte[] dhPrivateKey)
        {
            throw new NotImplementedException();
        }
    }
}
