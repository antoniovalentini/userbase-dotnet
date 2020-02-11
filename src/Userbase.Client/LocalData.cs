using System;
using Newtonsoft.Json;

namespace Userbase.Client
{
    public class LocalData
    {
        private readonly IStorage _sessionStorage;
        private readonly IStorage _localStorage;

        public LocalData(IStorage sessionStorage, IStorage localStorage)
        {
            _sessionStorage = sessionStorage;
            _localStorage = localStorage;
        }

        public string GetSeedName(string appId, string username)
        {
            return $"userbaseSeed.{appId}.{username}";
        }

        public string GetSeedString(string appId, string username)
        {
            return TryCatchWrapperFunc(() => { 
                var seedName = GetSeedName(appId, username);
                var sessionItem = _sessionStorage.GetItem(seedName);
                return !string.IsNullOrEmpty(sessionItem) ? sessionItem : _localStorage.GetItem(seedName);
            });
        }

        public void SignInSession(string rememberMe, string username, string sessionId, string creationDate)
        {
            TryCatchWrapperAction(() => SetCurrentSession(rememberMe, username, true, sessionId, creationDate));
        }

        private void SetCurrentSession(string rememberMe, string username, bool signedIn, string sessionId, string creationDate)
        {
            var session = new {username, signedIn, sessionId, creationDate};
            var sessionString = JsonConvert.SerializeObject(session);

            if (rememberMe == "local")
            {
                _localStorage.SetItem("userbaseCurrentSession", sessionString);
            } else if (rememberMe == "session")
            {
                _sessionStorage.SetItem("userbaseCurrentSession", sessionString);
            }
        }

        private static void TryCatchWrapperAction(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                // ok to swallow error
                //
                // local/sessionStorage are non-critical benefits. If they happen to be available,
                // they're helpful, but if not, the SDK functions totally fine.
                //
                // If a function fails, behavior is functionally the same as if rememberMe is 'none'.
                // TODO: Defaulting to memory? How?
                System.Diagnostics.Trace.TraceWarning($"Error accessing browser storage. Defaulting to memory. Error: {e.Message}");
            }
        }
        private static TResult TryCatchWrapperFunc<TResult>(Func<TResult> func)
        {
            try
            {
                return func.Invoke();
            }
            catch (Exception e)
            {
                // ok to swallow error
                //
                // local/sessionStorage are non-critical benefits. If they happen to be available,
                // they're helpful, but if not, the SDK functions totally fine.
                //
                // If a function fails, behavior is functionally the same as if rememberMe is 'none'.
                // TODO: Defaulting to memory? How?
                System.Diagnostics.Trace.TraceWarning($"Error accessing browser storage. Defaulting to memory. Error: {e.Message}");
                return default;
            }
        }
    }

    public interface IStorage
    {
        string GetItem(string key);
        void SetItem(string key, string value);
    }
}
