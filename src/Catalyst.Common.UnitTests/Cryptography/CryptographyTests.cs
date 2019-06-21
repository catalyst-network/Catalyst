#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Text;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Cryptography.BulletProofs.Wrapper.Exceptions;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using FluentAssertions;
using Xunit;

namespace Catalyst.Common.UnitTests.Cryptography
{
    public sealed class CryptographyTests
    {
        public CryptographyTests() { _context = new RustCryptoContext(); }

        private readonly ICryptoContext _context;

        [Fact]
        public void TestGeneratePrivateKey()
        {
            var privateKey = _context.GeneratePrivateKey();
            privateKey.Should().BeOfType(typeof(PrivateKey));
        }

        [Fact]
        public void TestFailureSigningVerification()
        {
            var key1 = _context.GeneratePrivateKey();

            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signature = _context.Sign(key1, data);

            var key2 = _context.GeneratePrivateKey();
            var publicKey2 = _context.GetPublicKey(key2);

            _context.Verify(publicKey2, data, signature)
               .Should().BeFalse("signature should not verify with incorrect key");
        }

        [Fact]
        public void TestPublicKeyFromPrivateKey()
        {
            var privateKey = _context.GeneratePrivateKey();
            var publicKey = _context.GetPublicKey(privateKey);

            publicKey.Should().NotBeNull(" a valid public key should be created from a private key");
        }

        [Fact]
        public void TestSigningVerification()
        {
            var privateKey = _context.GeneratePrivateKey();
            var publicKey = _context.GetPublicKey(privateKey);
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signature = _context.Sign(privateKey, data);

            _context.Verify(publicKey, data, signature)
               .Should().BeTrue("signature generated with private key should verify with corresponding public key");
        }

        [Fact]
        public void TestVerifyWithImportedPublicKey()
        {
            var privateKey = _context.GeneratePrivateKey();
            var publicKey = _context.GetPublicKey(privateKey);
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signature = _context.Sign(privateKey, data);
            var blob = _context.ExportPublicKey(publicKey);

            var importedKey = _context.ImportPublicKey(blob);
            _context.Verify(importedKey, data, signature).Should()
               .BeTrue("signature should verify with imported public key");
        }

        [Fact] 
        public void Can_Throw_Signature_Exception_On_Invalid_Signature()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            IPublicKey publicKey = _context.GetPublicKey(privateKey);
            string invalidSignature = "mL9Z+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIA==";
            byte[] signatureBytes = Convert.FromBase64String(invalidSignature);
            Signature invalidSig = new Signature(signatureBytes);
            byte[] message = Encoding.UTF8.GetBytes("fa la la la");
            Action action = () => { _context.Verify(publicKey, message, invalidSig); };
            action.Should().Throw<SignatureException>();
        }
    }
}
