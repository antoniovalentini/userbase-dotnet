namespace Userbase.Client.Ws.Models
{
    public class ConnectionMessage
    {
        public string Route;
        public KeySalts KeySalts;
        public EncryptedValidationMessage EncryptedValidationMessage;
    }
}
