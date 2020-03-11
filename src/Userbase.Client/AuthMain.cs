using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Userbase.Client.Api;
using Userbase.Client.Crypto;
using Userbase.Client.Errors;
using Userbase.Client.Models;
using Userbase.Client.Ws;

namespace Userbase.Client
{
    public class AuthMain
    {
        // move into config?
        private const int MaxPasswordCharLength = 1000;
        private const int MinPasswordCharLength = 6;

        private readonly Config _config;
        private readonly AuthApi _api;
        private readonly LocalData _localData;
        private readonly ILogger _logger;
        private readonly WsWrapper _ws;

        public AuthMain(Config config, AuthApi api, LocalData localData, ILogger logger)
        {
            _config = config;
            _api = api;
            _localData = localData;
            _logger = logger;
            _ws = new Ws.WsWrapper(_config, api, _logger);
        }

        public async Task<SignUpResult> SignUp(SignUpRequest signUpRequest)
        {
            try
            {
                ValidateSignUpOrSignInInput(signUpRequest.Username, signUpRequest.Password);
                if (signUpRequest.Profile != null) ValidateProfile(signUpRequest.Profile);
                if (string.IsNullOrEmpty(signUpRequest.Email)) throw new Errors.EmailNotValid();
                if (!Config.RememberMeOptions.ContainsKey(signUpRequest.RememberMe))
                    throw new RememberMeValueNotValid();

                var username = signUpRequest.Username.ToLower();
                var email = signUpRequest.Email.ToLower();
                var appId = _config.AppId;
                var seed = Utils.GenerateSeed();

                var (sessionId, creationDate, userId) =
                    await GenerateKeysAndSignUp(username, signUpRequest.Password, seed, email, signUpRequest.Profile);

                var session = new SignInSession
                {
                    Username = username, SessionId = sessionId,
                    CreationDate = creationDate.ToString(CultureInfo.InvariantCulture)
                };
                var seedString = Convert.ToBase64String(seed);

                _localData.SaveSeedString(signUpRequest.RememberMe, appId, username, seedString);
                _localData.SignInSession(signUpRequest.RememberMe, username, sessionId,
                    creationDate.ToString(CultureInfo.InvariantCulture));

                await ConnectWebSocket(session, seedString, signUpRequest.RememberMe);

                return new SignUpResult
                {
                    Username = username,
                    UserId = userId,
                    Email = email,
                    Profile = signUpRequest.Profile,
                };
            }
            catch (Exception ex)
            {
                if (!(ex is IError e)) throw new UnknownServiceUnavailable(ex);
                switch (e.Name)
                {
                    case "ParamsMustBeObject":
                    case "UsernameMissing":
                    case "UsernameAlreadyExists":
                    case "UsernameCannotBeBlank":
                    case "UsernameMustBeString":
                    case "UsernameTooLong":
                    case "PasswordMissing":
                    case "PasswordCannotBeBlank":
                    case "PasswordTooShort":
                    case "PasswordTooLong":
                    case "PasswordMustBeString":
                    case "EmailNotValid":
                    case "ProfileMustBeObject":
                    case "ProfileCannotBeEmpty":
                    case "ProfileHasTooManyKeys":
                    case "ProfileKeyMustBeString":
                    case "ProfileKeyTooLong":
                    case "ProfileValueMustBeString":
                    case "ProfileValueCannotBeBlank":
                    case "ProfileValueTooLong":
                    case "RememberMeValueNotValid":
                    case "TrialExceededLimit":
                    case "AppIdNotSet":
                    case "AppIdNotValid":
                    case "UserAlreadySignedIn":
                    case "ServiceUnavailable":
                        throw;

                    default:
                        throw new UnknownServiceUnavailable(ex);
                }
            }
        }

        public async Task<(string sessionId, DateTime creationDate, string userId)> GenerateKeysAndSignUp(
            string username, string password, byte[] seed, string email, Dictionary<string, string> profile)
        {
            var (passwordToken,passwordSalts,passwordBasedBackup) = GeneratePasswordToken(password, seed);

            var encryptionKeySalt = Hkdf.GenerateSalt();
            var dhKeySalt = Hkdf.GenerateSalt();
            var hmacKeySalt = Hkdf.GenerateSalt();

            var dhPrivateKey = DiffieHellmanUtils.ImportKeyFromMaster(seed, dhKeySalt);
            var publicKey = DiffieHellmanUtils.GetPublicKey(dhPrivateKey);

            var keySalts = new
            {
                encryptionKeySalt = Convert.ToBase64String(encryptionKeySalt),
                dhKeySalt = Convert.ToBase64String(dhKeySalt),
                hmacKeySalt = Convert.ToBase64String(hmacKeySalt),
            };
            
            var request = new SignUpApiRequest
            {
                Username = username,
                PasswordToken = passwordToken,
                PublicKey = publicKey,
                PasswordSalts = passwordSalts,
                KeySalts  = keySalts,
                Email = email,
                Profile = profile,
                PasswordBasedBackup = passwordBasedBackup
            };
            var response = await _api.SignUp(request);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonConvert.DeserializeObject<SignUpApiResponse>(await response.Content.ReadAsStringAsync());
                return (apiResponse.SessionId, apiResponse.CreationDate, apiResponse.UserId);
            }

            var one = await ParseGenericErrors(response);
            if (one != null)
                throw one;

            var two = await ParseGenericUsernamePasswordError(response);
            if (two != null)
                throw two;

            throw new Exception($"Unknown error during SignUp: {response.StatusCode}");
        }

        private (string passwordToken, dynamic passwordSalts, dynamic passwordBasedBackup) GeneratePasswordToken(string password, byte[] seed)
        {
            var passwordSalt = Scrypt.GenerateSalt();
            var passwordHash = Scrypt.Hash(password, passwordSalt);

            //var passwordHkdfKey = crypto.hkdf.importHkdfKeyFromString(passwordHash)
            var passwordTokenSalt = Hkdf.GenerateSalt();
            var passwordToken = Hkdf.GetPasswordToken( /*passwordHkdfKey*/passwordHash, passwordTokenSalt);

            var passwordBasedEncryptionKeySalt = Hkdf.GenerateSalt();
            var passwordBasedEncryptionKey = AesGcmUtils.GetPasswordBasedEncryptionKey(passwordHash, passwordBasedEncryptionKeySalt);

            var passwordEncryptedSeed = AesGcmUtils.Encrypt(passwordBasedEncryptionKey, seed);

            var passwordSalts = new 
            {
                passwordSalt = Convert.ToBase64String(passwordSalt),
                passwordTokenSalt = Convert.ToBase64String(passwordTokenSalt),
            };

            var passwordBasedBackup = new 
            {
                passwordBasedEncryptionKeySalt = Convert.ToBase64String(passwordBasedEncryptionKeySalt),
                passwordEncryptedSeed = Convert.ToBase64String(passwordEncryptedSeed),
            };

            return (passwordToken, passwordSalts, passwordBasedBackup);
        }

        public async Task<SignInResponse> SignIn(SignInRequest signInRequest)
        {
            try
            {
                ValidateSignUpOrSignInInput(signInRequest.Username, signInRequest.Password);
                ValidateRememberMeInput(signInRequest.RememberMe);

                var lowerCaseUsername = signInRequest.Username.ToLower();
                var passwordSalts = await GetPasswordSaltsOverRestEndpoint(lowerCaseUsername);
                // should it be awaited?
                var rebuildPasswordTokenResponse = RebuildPasswordToken(signInRequest.Password, passwordSalts);

                var signInDto = await ActualSignIn(lowerCaseUsername, rebuildPasswordTokenResponse.PasswordToken);
                var savedSeedString = _localData.GetSeedString(_config.AppId, lowerCaseUsername);

                var seedStringFromBackup = string.Empty;
                // TODO: usedTempPassword
                if (string.IsNullOrEmpty(savedSeedString))
                {
                    seedStringFromBackup = AesGcmUtils.GetSeedStringFromPasswordBasedBackup(rebuildPasswordTokenResponse.PasswordHkdfKey, signInDto.PasswordBasedBackup);
                    _localData.SaveSeedString(signInRequest.RememberMe, _config.AppId, lowerCaseUsername, seedStringFromBackup);
                }

                var seedString = !string.IsNullOrEmpty(savedSeedString) 
                    ? savedSeedString 
                    : seedStringFromBackup;

                _localData.SignInSession(signInRequest.RememberMe, lowerCaseUsername, signInDto.Session.SessionId,
                    signInDto.Session.CreationDate);

                // TODO
                await ConnectWebSocket(signInDto.Session, seedString, signInRequest.RememberMe);

                // TODO usedTempPassword
                return new SignInResponse
                {
                    Username = lowerCaseUsername,
                    UserId = signInDto.UserId,
                    Email = signInDto.Email,
                    Profile = signInDto.Profile,
                    InternalProfile = signInDto.InternalProfile,
                };
            }
            catch (Exception ex)
            {
                if (!(ex is IError e)) throw new UnknownServiceUnavailable(ex);
                switch (e.Name) {
                    case "ParamsMustBeObject":
                    case "UsernameOrPasswordMismatch":
                    case "UsernameCannotBeBlank":
                    case "UsernameTooLong":
                    case "UsernameMustBeString":
                    case "PasswordCannotBeBlank":
                    case "PasswordTooShort":
                    case "PasswordTooLong":
                    case "PasswordMustBeString":
                    case "PasswordAttemptLimitExceeded":
                    case "RememberMeValueNotValid":
                    case "AppIdNotSet":
                    case "AppIdNotValid":
                    case "UserAlreadySignedIn":
                    case "ServiceUnavailable":
                        throw;

                    default:
                        throw new UnknownServiceUnavailable(ex);
                }
            }
            
        }

        private async Task ConnectWebSocket(SignInSession session, string seed, string rememberMe)
        {
            HttpResponseMessage response;
            try
            {
                response = await _ws.Connect(session, seed, rememberMe);
            } 
            catch (Exception ex)
            {
                if (ex.Message == "Web Socket already connected")
                    throw new UserAlreadySignedIn(session.Username);

                throw;
            }

            if (response.IsSuccessStatusCode) return;

            var exception = await ParseGenericErrors(response);
            if (exception != null)
                throw exception;

            throw new Exception($"Unknown error during SignIn: {response.StatusCode}");
        }

        public async Task SignOut()
        {
            try
            {
                if (string.IsNullOrEmpty(_ws.Session.Username)) throw new UserNotSignedIn();

                // TODO
                await _ws.SignOut();
            }
            catch (Exception ex)
            {
                if (!(ex is IError e)) throw new UnknownServiceUnavailable(ex);
                switch (e.Name) {
                    case "UserNotSignedIn":
                    case "ServiceUnavailable":
                        throw;

                    default:
                        throw new UnknownServiceUnavailable(ex);
                }
            }
        }

        public static RebuildPasswordTokenResponse RebuildPasswordToken(string password, GetPasswordSaltsResponse salts)
        {
            var passwordhash = Scrypt.Hash(password, salts.PasswordSalt);

            var ikm = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(passwordhash));
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes("password-token"));
            var salt = Convert.FromBase64String(salts.PasswordTokenSalt);
            var passwordToken = new Hkdf().DeriveKey(salt, ikm, info, 32);

            return new RebuildPasswordTokenResponse
            {
                PasswordHkdfKey = ikm,
                PasswordToken = Convert.ToBase64String(passwordToken),
            };
        }

        private async Task<SignInDto> ActualSignIn(string lowerCaseUsername, string passwordToken)
        {
            HttpResponseMessage response;
            try
            {
                response = await _api.SignIn(lowerCaseUsername, passwordToken);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("timeout"))
                    throw new Timeout();
                throw;
            }

            if (response.IsSuccessStatusCode)
            {
                var signinDto = JsonConvert.DeserializeObject<SignInDto>(await response.Content.ReadAsStringAsync());
                signinDto.Session.Username = lowerCaseUsername;
                return signinDto;
            }

            var one = await ParseGenericErrors(response);
            if (one != null)
                throw one;

            var two = await ParseGenericUsernamePasswordError(response);
            if (two != null)
                throw two;

            if (await response.Content.ReadAsStringAsync() == "Invalid password")
                throw new UsernameOrPasswordMismatch();

            throw new Exception($"Unknown error during SignIn: {response.StatusCode}");
        }

        private static void ValidateProfile(Dictionary<string, string> profile)
        {
            if (profile.Keys.Count == 0) throw new ProfileCannotBeEmpty();
            foreach (var (key, value) in profile)
                if (string.IsNullOrEmpty(value)) throw new ProfileValueCannotBeBlank(key);
        }

        private static void ValidateSignUpOrSignInInput(string username, string password)
        {
            // USERNAME
            if (string.IsNullOrEmpty(username)) throw new UsernameCannotBeBlank();

            // PASSWORD
            if (string.IsNullOrEmpty(password)) throw new PasswordCannotBeBlank();
            if (password.Length < MinPasswordCharLength) throw new PasswordTooShort(MinPasswordCharLength);
            if (password.Length > MaxPasswordCharLength) throw new PasswordTooLong(MaxPasswordCharLength);
        }

        private static void ValidateRememberMeInput(string rememberMe)
        {
            // REMEMBER ME
            if (rememberMe == null || !Config.RememberMeOptions.ContainsKey(rememberMe)) throw new RememberMeValueNotValid();
        }

        public async Task<GetPasswordSaltsResponse> GetPasswordSaltsOverRestEndpoint(string username)
        {
            HttpResponseMessage response;
            try
            {
                response = await _api.GetPasswordSalts(username);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("timeout"))
                    throw new Timeout();
                throw;
            }

            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<GetPasswordSaltsResponse>(
                    await response.Content.ReadAsStringAsync());

            var one = await ParseGenericErrors(response);
            if (one != null)
                throw one;

            var two = await ParseGenericUsernamePasswordError(response);
            if (two != null)
                throw two;

            if (await response.Content.ReadAsStringAsync() == "User not found")
                throw new UsernameOrPasswordMismatch();

            throw new Exception($"Unknown error when fetching password salts: {response.StatusCode}");
        }

        public static async Task<Exception> ParseGenericErrors(HttpResponseMessage response)
        {
            var data = await response.Content.ReadAsStringAsync();
            if (data == "App ID not valid")
                return new AppIdNotValid(response.StatusCode);
            if (data == "UserNotFound")
                return new UserNotFound();
            if (response.StatusCode == HttpStatusCode.InternalServerError)
                return new InternalServerError();
            if (response.StatusCode == HttpStatusCode.GatewayTimeout)
                return new Timeout();
            return null;
        }

        private static async Task<Exception> ParseGenericUsernamePasswordError(HttpResponseMessage response)
        {
            var data = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(data);
            if (json["error"].ToString() == "UsernameTooLong")
                return new UsernameTooLong(json["maxLen"].ToString());
            if (json["error"].ToString() == "PasswordAttemptLimitExceeded")
                return new PasswordAttemptLimitExceeded(json["delay"].ToString());
            return null;
        }
    }
}