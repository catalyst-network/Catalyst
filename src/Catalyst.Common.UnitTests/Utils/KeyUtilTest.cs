using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using FluentAssertions;
using Xunit;

namespace Catalyst.Common.UnitTests.Utils
{
    public class KeyUtilTest
    {
        [Fact]
        public void Can_Encode_And_Decode_Correctly()
        {
            var cryptoContext = new CryptoContext(new CryptoWrapper());
            var privateKey = cryptoContext.GeneratePrivateKey();
            var publicKey = privateKey.GetPublicKey();

            var publicKeyAfterEncodeDecode = publicKey.Bytes.KeyToString().KeyToBytes();
            publicKeyAfterEncodeDecode.Should().BeEquivalentTo(publicKey.Bytes);
        }
    }
}
