namespace Userbase.Client.Ws.Models
{
    public class Keys
    {
        public bool Init = false;
        public KeySalts Salts;
        public byte[] EncryptionKey;
        public byte[] DhPrivateKey;
        public byte[] HmacKey;

        public void Clear()
        {
            Init = false;
            Salts = new KeySalts();
            EncryptionKey = null;
            DhPrivateKey = null;
            HmacKey = null;
        }
    }
}
