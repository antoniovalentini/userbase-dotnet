using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Userbase.Client.Crypto
{
    public class DiffieHellmanUtils
    {
        // RFC 3526 detailing publicly known 2048 bit safe prime: https://www.ietf.org/rfc/rfc3526.txt
        private static byte[] PRIME = HexStringToBytes(
            "ffffffffffffffffc90fdaa22168c234c4c6628b80dc1cd129024e088a67cc74020bbea63b139b22514a08798e3404ddef9519b3cd3a431b302b0a6df25f14374fe1356d6d51c245e485b576625e7ec6f44c42e9a637ed6b0bff5cb6f406b7edee386bfb5a899fa5ae9f24117c4b1fe649286651ece45b3dc2007cb8a163bf0598da48361c55d39a69163fa8fd24cf5f83655d23dca3ad961c62f356208552bb9ed529077096966d670c354e4abc9804f1746c08ca18217c32905e462e36ce3be39e772c180e86039b2783a2ec07a28fb5c55df06f4c52c9de2bcbf6955817183995497cea956ae515d2261898fa051015728e5a8aacaa68ffffffffffffffff");
        private static int[] GENERATOR = {2};
        private const string DiffieHellmanKeyName = "diffie-hellman";
        public static byte[] ImportKeyFromMaster(byte[] masterKey, byte[] dhKeySalt)
        {
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(DiffieHellmanKeyName));
            return new Hkdf().DeriveKey(dhKeySalt, masterKey, info, 32);
        }

        public static byte[] GetSharedKeyWithServer(byte[] privateKey, byte[] serverPublicKey)
        {
            //var key = CngKey.Create(CngAlgorithm.Sha256, DiffieHellmanKeyName, new CngKeyCreationParameters());

            //var curve = new ECCurve
            //{
            //    G = new ECPoint
            //    {

            //    }
            //};
            //var g = new ECPoint {X = ((BigInteger) GENERATOR[0]).ToByteArray()};
            //curve.G = g;
            //curve.Prime = PRIME;

            try
            {
                //using (var cng = new ECDiffieHellmanCng(CngKey.Import(privateKey, CngKeyBlobFormat.Pkcs8PrivateBlob)))
                //{
                //    byte[] aliceKey = cng.DeriveKeyMaterial(CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob));
                //}

                //CngKey k = null;
                //var errors = new List<string>();
                //try { k = CngKey.Import(privateKey, CngKeyBlobFormat.EccPublicBlob); }
                //catch (Exception e) { errors.Add(e.Message); }
                //try { k = CngKey.Import(privateKey, CngKeyBlobFormat.EccPrivateBlob); }
                //catch (Exception e) { errors.Add(e.Message); }
                //try { k = CngKey.Import(privateKey, CngKeyBlobFormat.EccFullPublicBlob); }
                //catch (Exception e) { errors.Add(e.Message); }
                //try { k = CngKey.Import(privateKey, CngKeyBlobFormat.EccFullPrivateBlob); }
                //catch (Exception e) { errors.Add(e.Message); }
                //try { k = CngKey.Import(privateKey, CngKeyBlobFormat.GenericPublicBlob); }
                //catch (Exception e) { errors.Add(e.Message); }
                //try { k = CngKey.Import(privateKey, CngKeyBlobFormat.GenericPrivateBlob); }
                //catch (Exception e) { errors.Add(e.Message); }
                //try { k = CngKey.Import(privateKey, CngKeyBlobFormat.Pkcs8PrivateBlob); }
                //catch (Exception e) { errors.Add(e.Message); }
                //try { k = CngKey.Import(privateKey, CngKeyBlobFormat.OpaqueTransportBlob); }
                //catch (Exception e) { errors.Add(e.Message); }

                using (var alice = new ECDiffieHellmanCng())
                {
                    alice.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                    alice.HashAlgorithm = CngAlgorithm.Sha256;
                    //var alicePublicKey = alice.PublicKey.ToByteArray();
                    //CngKey pubKey = CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob);
                    byte[] aliceKey = alice.DeriveKeyMaterial(new UserbaseKey(serverPublicKey, PRIME, null));
                    return aliceKey;
                }

                

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            //using (var alice  = new ECDiffieHellmanCng(curve))
            //{
            //    alice.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            //    alice.HashAlgorithm = CngAlgorithm.Sha256;
            //    var alicePublicKey = alice.PublicKey.ToByteArray();
            //    CngKey k = CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob);
            //    byte[] aliceKey = alice.DeriveKeyMaterial(CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob));
            //    byte[] encryptedMessage = null;
            //    byte[] iv = null;
            //    //Send(aliceKey, "Secret message", out encryptedMessage, out iv);
            //    //bob.Receive(encryptedMessage, iv);
            //}
            //const diffieHellman = createDiffieHellman(privateKey)
            //const sharedSecret = diffieHellman.computeSecret(otherPublicKey)

            //const sharedRawKey = await sha256.hash(sharedSecret)
            //const sharedKey = await aesGcm.getKeyFromRawKey(sharedRawKey)
            //return sharedKey;
            return null;
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
    }

    public class UserbaseKey : ECDiffieHellmanPublicKey
    {
        private readonly byte[] _privateKey;
        private readonly byte[] _prime;
        private readonly byte[] _gen;

        public UserbaseKey(byte[] privateKey, byte[] prime, byte[] gen)
        {
            _privateKey = privateKey;
            _prime = prime;
            _gen = gen;
        }

        public override ECParameters ExportParameters()
        {
            return new ECParameters
            {
                D = _privateKey,
                Q = new ECPoint(),
                Curve = new ECCurve
                {
                    Prime = _prime,
                    
                }
            };
        }
    }
}
