using System.Security;
using Catalyst.Core.Lib.Cryptography;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Cryptography
{
    public class SecureStringExtensionsTest
    {
        [Fact]
        public void UseBytes()
        {
            var secret = new SecureString();
            var expected = new char[]
            {
                'a', 'b', 'c'
            };
            
            foreach (var c in expected)
            {
                secret.AppendChar(c);
            }
            
            secret.UseSecretBytes(bytes =>
            {
                Assert.Equal<int>(expected.Length, bytes.Length);
                for (var i = 0; i < expected.Length; ++i)
                {
                    Assert.Equal((int) expected[i], (int) bytes[i]);
                }
            });
        }
    }
}
