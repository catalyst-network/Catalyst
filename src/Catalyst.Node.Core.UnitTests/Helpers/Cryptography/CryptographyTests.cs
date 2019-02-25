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
        private readonly ICryptoContext _context;

        public CryptographyTests()
        {
            _context = new NSecCryptoContext();
        }
        [Fact]
        public void TestSigningVerification(){
            
            
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            byte[] signature = _context.Sign(privateKey, data);
            _context.Verify(privateKey, data, signature).Should().BeTrue("signature generated with private key should verify with corresponding public key");
 
        }
        
        [Fact]
        public void TestFailureSigningVerification(){
            
            
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            byte[] signature = _context.Sign(privateKey, data);
            
            IPrivateKey key2 = _context.GeneratePrivateKey();
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
        public void TestPublicKeyImport()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            byte[] blob = _context.ExportPublicKey(privateKey);
            
            IPublicKey publicKey = _context.ImportPublicKey(blob);
            publicKey.Should().NotBeNull("public key should be importable from a valid blob");

        }
        
        [Fact]
        public void TestFailurePrivateKeyImport()
        {
            var blob = Encoding.UTF8.GetBytes("this string is not a formatted private key");
            
            Action act = () => _context.ImportPrivateKey(blob);

            act.Should().Throw<ArgumentNullException>("invalid private key import should result in null value");

        }

        [Fact]
        public void TestPrivateKeyExport()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            byte[] blob = _context.ExportPrivateKey(privateKey);
            blob.Should().NotBeNull("newly generated private key should be exportable once");

        }
        
        [Fact]
        public void TestPrivateKeyImportFromExported()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            byte[] blob = _context.ExportPrivateKey(privateKey);
            
            IPublicKey importedPrivateKey = _context.ImportPrivateKey(blob);
            importedPrivateKey.Should().NotBeNull("private key should be importable from a valid blob");

        }

        [Fact]
        public void TestFailurePrivateKeySecondExport()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            byte[] blob = _context.ExportPrivateKey(privateKey);
            byte[] blob2 = _context.ExportPrivateKey(privateKey);
            blob.Should().BeNull("newly generated private key should not be exportable more than once");
            
            
        }

        [Fact]
        public void TestPublicKeyFromPrivateKey()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            IPublicKey publicKey = _context.GetPublicKey(privateKey);
            publicKey.Should().NotBeNull(" a valid public key should be created from a private key");
            
        }

        [Fact]
        public void TestVerifyWithImportedPublicKey()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            byte[] signature = _context.Sign(privateKey, data);
            
            byte[] blob = _context.ExportPublicKey(privateKey);
            IPublicKey importedKey = _context.ImportPublicKey(blob);
            _context.Verify(importedKey, data, signature).Should().BeTrue("signature should verify with imported public key");
            
        }
       
    }
    
}
