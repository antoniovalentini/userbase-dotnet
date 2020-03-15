using System.Collections.Generic;
using Userbase.Client.Data;

namespace Userbase.Client.Ws.Models
{
    public class UserState
    {
        public Dictionary<string, Database> Databases;
        public Dictionary<string, string> DbIdToHash;
        public Dictionary<string, string> DbNameToHash;
    }
}
