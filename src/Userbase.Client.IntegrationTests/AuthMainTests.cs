using System.Threading.Tasks;
using Userbase.Client.Api;
using Userbase.Client.Models;
using Xunit;

namespace Userbase.Client.IntegrationTests
{
    public class AuthMainTests
    {
        /// <summary>
        /// In order to run this test you need to feel username, password and appId
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task SimpleTest()
        {
            // ARRANGE
            const string username = "";
            const string password = "";
            const string appId = "";
            var config = new Config(appId);
            var localData = new LocalData(new FakeLocalData(), new FakeLocalData());
            var auth = new AuthMain(config, new AuthApi(config), localData);
            var request = new SignInRequest {Username = username, Password = password, RememberMe = "none"};

            // ACT
            var response = await auth.SignIn(request);

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
