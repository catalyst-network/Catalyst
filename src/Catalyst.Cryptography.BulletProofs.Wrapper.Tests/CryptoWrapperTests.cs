#region LICENSE

/*
 * Copyright (c) 2019 Catalyst Network
 *
 * This file is part of Catalyst.Cryptography.BulletProofs.Wrapper <https://github.com/catalyst-network/Rust.Cryptography.FFI.Wrapper>
 *
 * Catalyst.Cryptography.BulletProofs.Wrapper is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * Catalyst.Cryptography.BulletProofs.Wrapper is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Catalyst.Cryptography.BulletProofs.Wrapper If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Text;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Exceptions;
using FluentAssertions;
using Nethereum.Hex.HexConvertors.Extensions;
using Xunit;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs.Tests
{
    public class CryptoWrapperTests
    {
        public CryptoWrapperTests() { _wrapper = new CryptoWrapper(); }

        private readonly IWrapper _wrapper;
        private static readonly Random _random = new Random();

        [Fact]
        public void TestGenerateKey()
        {
            IPrivateKey privateKey = _wrapper.GeneratePrivateKey();
            privateKey.Bytes.Length.Should().Be(_wrapper.PrivateKeyLength);
        }

        [Fact]
        public void TestGenerateDifferentKey()
        {
            IPrivateKey privateKey1 = _wrapper.GeneratePrivateKey();
            IPrivateKey privateKey2 = _wrapper.GeneratePrivateKey();
            privateKey1.Bytes.Should().NotEqual(privateKey2.Bytes);
        }

        [Fact]
        public void TestGetPublicKeyFromPrivate()
        {
            IPrivateKey privateKey = _wrapper.GeneratePrivateKey();
            IPublicKey publicKey = _wrapper.GetPublicKeyFromPrivate(privateKey);
            publicKey.Bytes.Length.Should().Be(_wrapper.PublicKeyLength);
        }

        [Fact]
        public void TestStdSignVerify()
        {
            IPrivateKey privateKey = _wrapper.GeneratePrivateKey();
            byte[] message = Encoding.UTF8.GetBytes("fa la la la");
            byte[] context = Encoding.UTF8.GetBytes("context");
            ISignature signature = _wrapper.StdSign(privateKey, message, context);
            bool isVerified = _wrapper.StdVerify(signature, message, context);
            isVerified.Should().BeTrue();
        }

        [Fact]
        public void TestLogicalFailureStdSignVerify()
        {
            IPrivateKey privateKey = _wrapper.GeneratePrivateKey();
            byte[] message1 = Encoding.UTF8.GetBytes("fa la la la");
            byte[] message2 = Encoding.UTF8.GetBytes("fa la la lahhhhhhh");
            byte[] context = Encoding.UTF8.GetBytes("any old context");
            ISignature signature = _wrapper.StdSign(privateKey, message1, context);
            bool isVerified = _wrapper.StdVerify(signature, message2, context);
            isVerified.Should().BeFalse();
        }

        //From https://tools.ietf.org/html/rfc8032#section-7.3
        [Theory]
        [InlineData("616263", "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406", "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", true)]
        [InlineData("616263", "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406", "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "a", false)]
        [InlineData("616261", "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406", "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", false)]
        [InlineData("616263", "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406", "0f1d1274943b91415889152e893d80e93275a1fc0b65fd71b4b0dda10ad7d772", "", false)]
        [InlineData("616263", "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083405", "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", false)]
        public void Ed25519phTestVectors(string message, string signature, string publicKey, string context, bool expectedResult)
        {
            var signatureAndMessage = signature.HexToByteArray();
            byte[] signatureBytes = new ArraySegment<byte>(signatureAndMessage, 0, _wrapper.SignatureLength).ToArray();
            byte[] publicKeyBytes = publicKey.HexToByteArray();
            byte[] messageBytes = message.HexToByteArray();
            byte[] contextBytes = context.HexToByteArray();
            
            var sig = _wrapper.SignatureFromBytes(signatureBytes, publicKeyBytes);
            
            var isVerified = _wrapper.StdVerify(sig, messageBytes, contextBytes);
            isVerified.Should().Be(expectedResult);
        }

        [Fact]
        public void TestFailureInvalidSignatureFormat()
        {
            IPrivateKey privateKey = _wrapper.GeneratePrivateKey();
            byte[] publicKeyBytes = _wrapper.GetPublicKeyFromPrivate(privateKey).Bytes;
            string invalidSignature = "mL9Z+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIA==";
            byte[] signatureBytes = Convert.FromBase64String(invalidSignature);
            ISignature invalidSig = _wrapper.SignatureFromBytes(signatureBytes, publicKeyBytes);
            byte[] message = Encoding.UTF8.GetBytes("fa la la la");
            Action action = () => { _wrapper.StdVerify(invalidSig, message, "".HexToByteArray()); };
            action.Should().Throw<SignatureException>();
        }

        [Fact]
        public void Is_PrivateKey_Length_Positive()
        {
            _wrapper.PrivateKeyLength.Should().BePositive();
        }

        [Fact]
        public void Is_PublicKey_Length_Positive()
        {
            _wrapper.PublicKeyLength.Should().BePositive();
        }

        [Fact]
        public void Is_Signature_Length_Positive()
        {
            _wrapper.SignatureLength.Should().BePositive();
        }

        [Fact]
        public void Is_Signature_Context_Max_Length_Positive()
        {
            _wrapper.SignatureContextMaxLength.Should().BePositive();
        }

        [Fact]
        public void PublicKey_Can_Be_Created_With_Valid_Input()
        {
            const string publicKeyHex = "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025";
            byte[] publicKeyBytes = publicKeyHex.HexToByteArray();
            IPublicKey publicKey = _wrapper.PublicKeyFromBytes(publicKeyBytes);
            publicKey.Should().NotBe(null);
        }

        [Fact]
        public void PublicKeyFromBytes_Throws_SignatureException_On_Invalid_Point()
        {
            const string publicKeyHex = "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908024";
            byte[] publicKeyBytes = publicKeyHex.HexToByteArray();
            Action action = () =>
            {
                _wrapper.PublicKeyFromBytes(publicKeyBytes);
            };
            action.Should().Throw<SignatureException>();
        }

        [Fact]
        public void PublicKeyFromBytes_Throws_ArgumentException_On_Invalid_Length_PublicKey()
        {
            var publicKeyBytes = GenerateRandomByteArray(_wrapper.PublicKeyLength - 1);
            Action action = () =>
            {
                _wrapper.PublicKeyFromBytes(publicKeyBytes);
            };
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void PrivateKey_Can_Be_Created_With_Valid_Input()
        {          
            var privateKeyBytes = GenerateRandomByteArray(_wrapper.PrivateKeyLength);
            IPrivateKey privateKey = _wrapper.PrivateKeyFromBytes(privateKeyBytes);
            privateKey.Should().NotBe(null);
        }

        [Fact]
        public void PrivateKeyFromBytes_Throws_ArgumentException_On_Invalid_Length_PrivateKey()
        {
            var privateKeyBytes = GenerateRandomByteArray(_wrapper.PrivateKeyLength + 1);
            Action action = () =>
            {
                _wrapper.PrivateKeyFromBytes(privateKeyBytes);
            };
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SignatureFromBytes_Throws_ArgumentException_On_Invalid_PublicKey_Length()
        {
            var signatureBytes = GenerateRandomByteArray(_wrapper.SignatureLength);
            var publicKeyBytes = GenerateRandomByteArray(_wrapper.PrivateKeyLength + 1);
            
            Action action = () =>
            {
                _wrapper.SignatureFromBytes(signatureBytes, publicKeyBytes);
            };

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SignatureFromBytes_Throws_ArgumentException_On_Invalid_Signature_Length()
        {
            var signatureBytes = GenerateRandomByteArray(_wrapper.SignatureLength - 1);

            var privateKey = _wrapper.GeneratePrivateKey();
            var publicKey = _wrapper.GetPublicKeyFromPrivate(privateKey);
            
            Action action = () =>
            {
                _wrapper.SignatureFromBytes(signatureBytes, publicKey.Bytes);
            };

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Can_Create_Signature_With_Valid_Input()
        {
            var signatureBytes = GenerateRandomByteArray(_wrapper.SignatureLength);

            var privateKey = _wrapper.GeneratePrivateKey();
            var publicKey = _wrapper.GetPublicKeyFromPrivate(privateKey);

            _wrapper.SignatureFromBytes(signatureBytes, publicKey.Bytes).Should().NotBe(null);
        }

        private static byte[] GenerateRandomByteArray(int length)
        {
            var buf = new byte[length];
            _random.NextBytes(buf);
            return buf;
        }
    }
}
