using System.Text;
using Catalyst.Node.Common.Cryptography;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Cryptography
{
    public class CryptographyTests
    {
        private readonly ICryptoContext _context;

        public CryptographyTests()
        {
            _context = new NSecCryptoContext();
        }
        [Fact]
        public void TestSigningVerification(){
            
            IKey key = _context.GenerateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            byte[] signature = _context.Sign(key, data);
            _context.Verify(key, data, signature).Should().BeTrue("signature generated with private key should verify with corresponding public key");
        }
        
        [Fact]
        public void TestFailureSigningVerification(){
            
            IKey key = _context.GenerateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            byte[] signature = _context.Sign(key, data);
            
            IKey key2 = _context.GenerateKey();
            _context.Verify(key2, data, signature).Should().BeFalse("signature should not verify with incorrect key");   
        }

        [Fact]
        public void TestFailurePublicKeyImport()
        {
            var blob = Encoding.UTF8.GetBytes("this string is not a formatted public key");

            IPublicKey publicKey = _context.ImportPublicKey(blob);
            publicKey.Should().BeNull("invalid public key import should result in null value");
        }

        [Fact]
        public void TestPublicKeyFormat()
        {
            IKey key = _context.GenerateKey();
            byte[] blob = _context.ExportPublicKey(key);
            
            IPublicKey publicKey = _context.ImportPublicKey(blob);
            publicKey.Should().NotBeNull();
        }

        [Fact]
        public void TestVerifyWithImportedPublicKey()
        {
            IKey key = _context.GenerateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            byte[] signature = _context.Sign(key, data);
            
            byte[] blob = _context.ExportPublicKey(key);
            IPublicKey importedKey = _context.ImportPublicKey(blob);
            _context.Verify(importedKey, data, signature).Should().BeTrue("signature should verify with imported public key");       
        }
    }
}
