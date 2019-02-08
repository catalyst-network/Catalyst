using Catalyst.Node.Core.Helpers.Cryptography;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.UnitTests.Helpers.Cryptography
{
    public class CryptographyTests
    {
        [Fact]
        public void CreateContext(){
            ICryptoContext context = new NSecCryptoContext();
            IKey key = context.GenerateKey();
            key.Should().BeOfType(typeof(NSecKey));
            key.GetNSecFormatKey().Should().BeOfType(typeof(NSec.Cryptography.Key));
        }
    }

}
