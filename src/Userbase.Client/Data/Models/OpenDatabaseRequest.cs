using System;
using System.Collections.Generic;

namespace Userbase.Client.Data.Models
{
    public class OpenDatabaseRequest
    {
        public string DatabaseName { get; set; }
        public Action<List<Database.Item>> ChangeHandler { get; set; }

        internal void Deconstruct(out string databaseName, out Action<List<Database.Item>> changeHandler)
        {
            databaseName = DatabaseName;
            changeHandler = ChangeHandler;
        }
    }
}
