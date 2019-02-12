using Catalyst.Node.Core.Helpers.Cryptography;
using FluentAssertions;
using Xunit;
using System.Text;
using System;
using NSec.Cryptography;
using Xunit.Abstractions;

namespace Catalyst.Node.UnitTests.Helpers.Cryptography
{
    public class CryptographyTests
    {
        private readonly ITestOutputHelper output;

        public CryptographyTests(ITestOutputHelper output)
        {
            this.output = output;
        }
        [Fact]
        public void TestNSecWrapperSigningVerification(){
            ICryptoContext context = new NSecCryptoContext();
            
            IKey key = context.GenerateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            byte[] signature = context.Sign(key, data);
            context.Verify(key, data, signature).Should().BeTrue("signature generated with private key should verify with corresponding public key");

            IKey key2 = context.GenerateKey();
            context.Verify(key2, data, signature).Should().BeFalse("signature should not verify with incorrect key");   
        }

        [Fact]
        public void TestIncorrectPublicKeyImport()
        {
            ICryptoContext context = new NSecCryptoContext();
            var blob = Encoding.UTF8.GetBytes("this string is not a formatted public key");

            IPublicKey publicKey = context.ImportPublicKey(blob);
            //publicKey.Empty.Should().BeTrue();

        }
       
    }
    
}
