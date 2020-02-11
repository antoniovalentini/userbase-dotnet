﻿using System;
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

        public AuthMain(Config config, AuthApi api, LocalData localData)
        {
            _config = config;
            _api = api;
            _localData = localData;
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
                var passwordToken = RebuildPasswordToken(signInRequest.Password, passwordSalts);

                var signInDto = await ActualSignIn(lowerCaseUsername, passwordToken);
                var savedSeedString = _localData.GetSeedString(_config.AppId, lowerCaseUsername);

                var seedStringFromBackup = string.Empty;
                if (string.IsNullOrEmpty(savedSeedString))
                {
                    // TODO
                    //seedStringFromBackup = await _getSeedStringFromPasswordBasedBackup(passwordHkdfKey, passwordBasedBackup);
                    //_localData.SaveSeedString(rememberMe, appId, lowerCaseUsername, seedStringFromBackup)
                }

                var seedString = !string.IsNullOrEmpty(savedSeedString) ? savedSeedString : seedStringFromBackup;
                _localData.SignInSession(signInRequest.RememberMe, lowerCaseUsername, signInDto.Session.SessionId,
                    signInDto.Session.CreationDate);

                // TODO
                //await _connectWebSocket(session, seedString, rememberMe)

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

        public static string RebuildPasswordToken(string password, GetPasswordSaltsResponse salts)
        {
            var passwordhash = Scrypt.Hash(password, salts.PasswordSalt);

            var ikm = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes(passwordhash));
            var info = Utils.FillOddsWithZeros(Encoding.ASCII.GetBytes("password-token"));
            var salt = Convert.FromBase64String(salts.PasswordTokenSalt);
            var passwordToken = new Hkdf().DeriveKey(salt, ikm, info, 32);

            return Convert.ToBase64String(passwordToken);
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
                return JsonConvert.DeserializeObject<SignInDto>(await response.Content.ReadAsStringAsync());

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

        private static async Task<Exception> ParseGenericErrors(HttpResponseMessage response)
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