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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Cryptography;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Exceptions;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using FluentAssertions;
using Nethereum.Hex.HexConvertors.Extensions;
using Xunit;

namespace Catalyst.Core.UnitTests.Cryptography
{
    public sealed class CryptographyTests
    {
        public CryptographyTests() { _context = new CryptoContext(new CryptoWrapper()); }

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
            var signingContext = Encoding.UTF8.GetBytes("Testing testing 1 2 3 context");
            var signature = _context.Sign(key1, data, signingContext);

            var key2 = _context.GeneratePrivateKey();
            var publicKey2 = _context.GetPublicKey(key2);

            var invalidSignature = _context.SignatureFromBytes(signature.SignatureBytes, publicKey2.Bytes);

            _context.Verify(invalidSignature, data, signingContext)
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
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signingContext = Encoding.UTF8.GetBytes("Testing testing 1 2 3 context");
            var signature = _context.Sign(privateKey, data, signingContext);

            _context.Verify(signature, data, signingContext)
               .Should().BeTrue("signature generated with private key should verify with corresponding public key");
        }

        [Fact]
        public void TestVerifyWithImportedPublicKey()
        {
            var privateKey = _context.GeneratePrivateKey();
            var publicKey = _context.GetPublicKey(privateKey);
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signingContext = Encoding.UTF8.GetBytes("Testing testing 1 2 3 context");
            var signature = _context.Sign(privateKey, data, signingContext);

            var blob = _context.ExportPublicKey(publicKey);

            var importedKey = _context.PublicKeyFromBytes(blob);
            var signatureWithImportedKey = _context.SignatureFromBytes(signature.SignatureBytes, importedKey.Bytes);
            _context.Verify(signatureWithImportedKey, data, signingContext).Should()
               .BeTrue("signature should verify with imported public key");
        }

        [Fact]
        public void Does_Verify_Fail_With_Incorrect_Signing_Context()
        {
            var privateKey = _context.GeneratePrivateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signingContext = Encoding.UTF8.GetBytes("Testing testing 1 2 3 context");
            var incorrectSigningContext = Encoding.UTF8.GetBytes("This is a different string");
 
            var signature = _context.Sign(privateKey, data, signingContext);

            _context.Verify(signature, data, incorrectSigningContext).Should()
               .BeFalse("signature should not verify with incorrect signing context");
        }

        [Fact] 
        public void Can_Throw_Signature_Exception_On_Invalid_Signature()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            IPublicKey publicKey = _context.GetPublicKey(privateKey);
            string invalidSignature = "mL9Z+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIA==";
            byte[] signatureBytes = Convert.FromBase64String(invalidSignature);
            var invalidSig = _context.SignatureFromBytes(signatureBytes, publicKey.Bytes);
            byte[] message = Encoding.UTF8.GetBytes("fa la la la");
            Action action = () => { _context.Verify(invalidSig, message, Encoding.UTF8.GetBytes("")); };
            action.Should().Throw<SignatureException>();
        }

        [Theory]
        [InlineData("616263",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406",
            "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", true)]
        [InlineData("616263",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406",
            "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "a", false)]
        [InlineData("616261",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406",
            "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", false)]
        [InlineData("616263",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406",
            "0f1d1274943b91415889152e893d80e93275a1fc0b65fd71b4b0dda10ad7d772", "", false)]
        [InlineData("616263",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083405",
            "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", false)]
        public void Verify_Validates_Correct_Test_Vector(string message,
            string signatureAndMessage,
            string publicKey,
            string context,
            bool expectedResult)
        {
            var signatureMessageBytes = signatureAndMessage.HexToByteArray();
            ArraySegment<byte> signatureBytes =
                new ArraySegment<byte>(signatureMessageBytes, 0, _context.SignatureLength);
            var publicKeyBytes = publicKey.HexToByteArray();
            var messageBytes = message.HexToByteArray();
            var contextBytes = Encoding.UTF8.GetBytes(context);
            var signature = _context.SignatureFromBytes(signatureBytes.Array, publicKeyBytes);
            
            _context.Verify(signature, messageBytes, contextBytes).Should().Be(expectedResult);
        }

        [Fact]
        public void Is_PrivateKey_Length_Positive()
        {
            _context.PrivateKeyLength.Should().BePositive();
        }

        [Fact]
        public void Is_PublicKey_Length_Positive()
        {
            _context.PublicKeyLength.Should().BePositive();
        }

        [Fact]
        public void Is_Signature_Length_Positive()
        {
            _context.SignatureLength.Should().BePositive();
        }

        [Fact]
        public void Is_Signature_Context_Max_Length_Positive()
        {
            _context.SignatureContextMaxLength.Should().BePositive();
        }
    }
}
