using System;
using System.Net;
using Userbase.Client.Errors;

namespace Userbase.Client.Data.Errors
{
    public class DatabaseNameCannotBeBlank : Exception, IError
    {
        public string Name => "DatabaseNameCannotBeBlank";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public DatabaseNameCannotBeBlank() : base("Database name cannot be blank.") { }
    }

    public class DatabaseNameTooLong : Exception, IError
    {
        public string Name => "DatabaseNameTooLong";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public DatabaseNameTooLong(int maxLength) : base($"Database name cannot be more than ${maxLength} characters.") { }
    }

    public class DatabaseAlreadyOpening : Exception, IError
    {
        public string Name => "";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public DatabaseAlreadyOpening() : base("Already attempting to open database.") { }
    }
}
