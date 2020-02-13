using System;
using Userbase.Client.Crypto;
using Xunit;

namespace Userbase.Client.UnitTests.Crypto
{
    public class AesGcmUtilsTests
    {
        [Fact]
        public void HashesShouldMatch()
        {
            // ARRANGE
            const string correctHash = "";
            byte[] passwordKeyHash = null;
            var passwordBasedEncryptionKeySalt = Convert.FromBase64String("");
            var aesGcm = new AesGcmUtils();

            // ACT
            var resultHash = aesGcm.GetSeedStringFromPasswordBasedBackup(passwordKeyHash, passwordBasedEncryptionKeySalt);

            // ASSERT
            Assert.Equal(correctHash, resultHash);
        }
    }
}
