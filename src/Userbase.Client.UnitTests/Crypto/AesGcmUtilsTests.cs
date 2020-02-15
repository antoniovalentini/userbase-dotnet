using Userbase.Client.Crypto;
using Userbase.Client.Models;
using Xunit;

namespace Userbase.Client.UnitTests.Crypto
{
    public class AesGcmUtilsTests
    {
        /// <summary>
        /// In order to run this test you need to fill a correct hash and a valid SignInPasswordBasedBackup
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void HashesShouldMatch()
        {
            // ARRANGE
            const string correctHash = "";
            byte[] passwordKeyHash = null;
            var passwordBasedBackup = new SignInPasswordBasedBackup
            {
                PasswordEncryptedSeed = "",
                PasswordBasedEncryptionKeySalt = "",
            };

            // ACT
            var resultHash = AesGcmUtils.GetSeedStringFromPasswordBasedBackup(passwordKeyHash, passwordBasedBackup);

            // ASSERT
            Assert.Equal(correctHash, resultHash);
        }
    }
}
