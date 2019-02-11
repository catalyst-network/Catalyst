using Catalyst.Node.Core.Helpers.Cryptography;
using FluentAssertions;
using Xunit;
using System.Text;

namespace Catalyst.Node.UnitTests.Helpers.Cryptography
{
    public class CryptographyTests
    {
        [Fact]
        public void TestNSecContextWrapperKeyGeneration(){
            ICryptoContext context = new NSecCryptoContext();
            IKey key = context.GenerateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            byte[] signature = context.Sign(key, data);
            context.Verify(key, data, signature).Should().BeTrue();
            
            IKey key2 = context.GenerateKey();
            context.Verify(key, data, signature).Should().BeFalse();   
            
        }

        
    }
    
    

}
