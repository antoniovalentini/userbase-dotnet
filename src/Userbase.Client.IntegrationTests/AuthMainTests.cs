using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Userbase.Client.Api;
using Userbase.Client.Data;
using Userbase.Client.Data.Models;
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
        public async Task SimpleSignIn()
        {
            // ARRANGE
            var username = Configuration["username"];
            var password = Configuration["password"];
            var appId = Configuration["appid"];
            var config = new Config(appId);
            var localData = new LocalData(new FakeLocalData(), new FakeLocalData());
            var logger = new TestLogger(_output);
            var api = new AuthApi(config);
            var ws = new WsWrapper(config, api, logger, localData);
            var auth = new AuthMain(config, api, localData, ws, logger);
            var request = new SignInRequest {Username = username, Password = password, RememberMe = "none"};

            // ACT
            var sw = Stopwatch.StartNew();
            var response = await auth.SignIn(request);
            while (ws.Instance4Net == null) {}
            while (ws.Instance4Net != null && ws.Instance4Net.State != WebSocketState.Closed)
            {
                if (sw.Elapsed.TotalSeconds > 120)
                    ws.Instance4Net.Close("Stop Test");
            }
            sw.Stop();

            // ASSERT
            Assert.NotNull(response.UserId);
            Assert.NotNull(response.Username);
        }

        [Fact]
        public async Task SimpleSignUp()
        {
            // ARRANGE
            var email = Configuration["email"];
            var username = Configuration["username"];
            var password = Configuration["password"];
            var appId = Configuration["appid"];

            var config = new Config(appId);
            var localData = new LocalData(new FakeLocalData(), new FakeLocalData());
            var logger = new TestLogger(_output);
            var api = new AuthApi(config);
            var ws = new WsWrapper(config, api, logger, localData);
            var auth = new AuthMain(config, api, localData, ws, logger);
            var request = new SignUpRequest {Username = username, Password = password, RememberMe = "none", Email = email};

            // ACT
            var response = await auth.SignUp(request);

            // ASSERT
            Assert.NotNull(response.UserId);
            Assert.NotNull(response.Username);
            Assert.NotNull(response.Email);
        }

        [Fact]
        public async Task CompleteTest() {
        
            // ARRANGE
            var username = Configuration["username"];
            var password = Configuration["password"];
            var appId = Configuration["appid"];

            var config = new Config(appId);
            var localData = new LocalData(new FakeLocalData(), new FakeLocalData());
            var logger = new TestLogger(_output);
            var api = new AuthApi(config);
            var ws = new WsWrapper(config, api, logger, localData);
            var auth = new AuthMain(config, api, localData, ws, logger);
            var signInRequest = new SignInRequest {Username = username, Password = password, RememberMe = "none"};

            var db = new Db(ws);
            void ChangeHandler(List<Database.Item> items)
            {
                var output = _output;
                output.WriteLine($"Received {items.Count} items from database.");
            }

            // ACT
            var response = await auth.SignIn(signInRequest);
            var promise = new TaskCompletionSource<int>();
            #pragma warning disable 4014
            Task.Factory.StartNew(() =>
            #pragma warning restore 4014
            {
                var scopedWs = ws;
                var scopedLogger = logger;
                while (!scopedWs.Keys.Init) {}
                scopedLogger.Log("KEYS INIT DONE");
                promise.SetResult(0);
            });

            await promise.Task;

            await db.OpenDatabase(new OpenDatabaseRequest {DatabaseName = "todos", ChangeHandler = ChangeHandler});

            // ASSERT
            Assert.NotNull(response.UserId);
            Assert.NotNull(response.Username);
            Assert.NotNull(response.Email);
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
