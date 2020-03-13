using System;
using System.Net;
using Newtonsoft.Json;

namespace Userbase.Client.Errors
{
    public interface IError
    {
        string Name { get; }
        HttpStatusCode Status { get; }
    }

    public class ProfileValueCannotBeBlank : Exception, IError
    {
        private readonly string _key;
        public string Name => "ProfileValueCannotBeBlank";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public ProfileValueCannotBeBlank(string key) : base("Profile value cannot be blank.")
        {
            _key = key;
        }
    }
    public class ProfileCannotBeEmpty : Exception, IError
    {
        public string Name => "ProfileCannotBeEmpty";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public ProfileCannotBeEmpty() : base("Profile cannot be empty.") {}
    }

    public class EmailNotValid : Exception, IError
    {
        public string Name => "EmailNotValid";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public EmailNotValid() : base("Email not valid.") {}
    }

    public class UsernameCannotBeBlank : Exception, IError
    {
        public string Name => "UsernameCannotBeBlank";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public UsernameCannotBeBlank() : base("Username cannot be blank.") {}
    }

    public class UsernameTooLong : Exception, IError
    {
        public string Name => "UsernameTooLong";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public UsernameTooLong(string maxLen) : base($"Username too long. Must be a max of ${maxLen} characters.") {}
    }

    public class PasswordCannotBeBlank : Exception, IError
    {
        public string Name => "PasswordCannotBeBlank";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public PasswordCannotBeBlank() : base("Password cannot be blank.") {}
    }

    public class PasswordTooShort : Exception, IError
    {
        public string Name => "PasswordTooShort";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public PasswordTooShort(int minLen) : base($"Password too short. Must be a minimum of {minLen} characters.") {}
    }

    public class PasswordTooLong : Exception, IError
    {
        public string Name => "PasswordTooLong";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public PasswordTooLong(int maxLen) : base($"Password too long. Must be a max of ${maxLen} characters.") {}
    }
    public class PasswordAttemptLimitExceeded : Exception, IError
    {
        public string Name => "PasswordAttemptLimitExceeded";
        public HttpStatusCode Status => HttpStatusCode.Unauthorized;
        public PasswordAttemptLimitExceeded(string delay) : base($"Password attempt limit exceeded. Must wait {delay} to attempt to use password again.") {}
    }

    public class RememberMeValueNotValid : Exception, IError
    {
        public string Name => "RememberMeValueNotValid";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public RememberMeValueNotValid() : base($"Remember me value must be one of {JsonConvert.SerializeObject(Config.RememberMeOptions)}") {}
    }

    public class UsernameOrPasswordMismatch : Exception, IError
    {
        public string Name => "UsernameOrPasswordMismatch";
        public HttpStatusCode Status => HttpStatusCode.Unauthorized;
        public UsernameOrPasswordMismatch() : base("Username or password mismatch.") {}
    }

    public class UserAlreadySignedIn : Exception, IError 
    {
        public string Name => "UserAlreadySignedIn";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public string Username { get; }
        public UserAlreadySignedIn(string username) : base("Already signed in.")
        {
            Username = username;
        }
    }

    public class AppIdNotValid : Exception, IError
    {
        public string Name => "AppIdNotValid";
        public HttpStatusCode Status { get; }
        public string Username { get; }

        public AppIdNotValid(HttpStatusCode statusCode, string username = "") : base("App ID not valid.")
        {
            Status = statusCode;
            Username = username;
        }
    }
    
    public class UserNotSignedIn : Exception, IError 
    {
        public string Name => "UserNotSignedIn";
        public HttpStatusCode Status => HttpStatusCode.BadRequest;
        public UserNotSignedIn() : base("Not signed in.") {}
    }

    public class UserNotFound : Exception, IError
    {
        public string Name => "UserNotFound";
        public HttpStatusCode Status => HttpStatusCode.NotFound;
        public UserNotFound() : base("User not found.") {}
    }

    public class ServiceUnavailable : Exception, IError
    {
        public string Name => "ServiceUnavailable";
        public HttpStatusCode Status { get; protected set; }
        public ServiceUnavailable() : base("Service unavailable.") {Status = HttpStatusCode.ServiceUnavailable;}
    }
    public class InternalServerError : ServiceUnavailable
    {
        public InternalServerError() {Status = HttpStatusCode.InternalServerError;}
    }
    public class Timeout : ServiceUnavailable
    {
        public Timeout() {Status = HttpStatusCode.GatewayTimeout;}
    }

    public class Reconnecting : Exception, IError
    {
        public string Name => "ServiceUnavailable";
        public HttpStatusCode Status => HttpStatusCode.ServiceUnavailable;
        public Reconnecting() : base("Reconnecting") {}
    }

    public class UnknownServiceUnavailable : Exception, IError
    {
        public string Name => "ServiceUnavailable";
        public HttpStatusCode Status => HttpStatusCode.ServiceUnavailable;

        public UnknownServiceUnavailable(Exception ex) : base("Unknown service unavailable.", ex)
        {
            System.Diagnostics.Trace.TraceError($"Userbase error. Please report this to support@userbase.com. Error: {ex.Message}");
        }
    }

    public class TooManyRequests : Exception, IError {
        public string Name => "TooManyRequests";
        public HttpStatusCode Status => HttpStatusCode.TooManyRequests;
        public TooManyRequests(float retryDelay) : this(CalculateDelay(retryDelay)) {}
        private TooManyRequests(int retryDelaySeconds) : base($"Too many requests in a row. Please try again in ${retryDelaySeconds} second{(retryDelaySeconds != 1 ? "s" : "")}.")
        {

        }
        private static int CalculateDelay(float retryDelay)
        {
            return (int)Math.Floor(retryDelay / 1000);
        }
    }
}
