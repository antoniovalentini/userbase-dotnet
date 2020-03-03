﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Userbase.Client.Api;
using Userbase.Client.Models;
using Userbase.Client.Ws;
using WebSocket4Net;
using Xunit;
using Xunit.Abstractions;

namespace Userbase.Client.IntegrationTests
{
    public class TestLogger : ILogger
    {
        private readonly ITestOutputHelper _output;

        public TestLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Log(string message)
        {
            _output.WriteLine($"{DateTime.Now} - {message}");
        }
    }

    public class AuthMainTests
    {
        private readonly ITestOutputHelper _output;

        private IConfiguration Configuration { get; }

        public AuthMainTests(ITestOutputHelper output)
        {
            _output = output;

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<AuthMainTests>();

            Configuration = builder.Build();
        }

        /// <summary>
        /// In order to run this test you need to fill username, password and appId
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task SimpleTest()
        {
            // ARRANGE
            var username = Configuration["username"];
            var password = Configuration["password"];
            var appId = Configuration["appid"];
            var config = new Config(appId);
            var localData = new LocalData(new FakeLocalData(), new FakeLocalData());
            var auth = new AuthMain(config, new AuthApi(config), localData, new TestLogger(_output));
            var request = new SignInRequest {Username = username, Password = password, RememberMe = "none"};

            // ACT
            var sw = Stopwatch.StartNew();
            var response = await auth.SignIn(request);
            while (WsWrapper.Instance4Net == null) {}
            while (WsWrapper.Instance4Net.State != WebSocketState.Closed)
            {
                if (sw.Elapsed.TotalSeconds > 120)
                    WsWrapper.Instance4Net.Close("Stop Test");
            }
            sw.Stop();

            // ASSERT
            Assert.NotNull(response.UserId);
            Assert.NotNull(response.Username);
        }
    }

    public class FakeLocalData : IStorage
    {
        public string GetItem(string key)
        {
            return string.Empty;
        }
        public void SetItem(string key, string value) {}
    }
}
