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
using System.Collections.Generic;
using System.Text;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Exceptions;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Google.Protobuf;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;


namespace Catalyst.Core.Modules.Cryptography.BulletProofs.Tests.UnitTests
{
    public sealed class CryptoWrapperTests
    {
        public CryptoWrapperTests() { _wrapper = new FfiWrapper(); }

        private readonly ICryptoContext _wrapper;
        private static readonly Random Random = new Random();

        [Test]
        public void TestGenerateKey()
        {
            var privateKey = _wrapper.GeneratePrivateKey();
            privateKey.Bytes.Length.Should().Be(_wrapper.PrivateKeyLength);
        }

        [Test]
        public void TestGenerateDifferentKey()
        {
            var privateKey1 = _wrapper.GeneratePrivateKey();
            var privateKey2 = _wrapper.GeneratePrivateKey();
            privateKey1.Bytes.Should().NotEqual(privateKey2.Bytes);
        }

        [Test]
        public void TestGetPublicKeyFromPrivate()
        {
            var privateKey = _wrapper.GeneratePrivateKey();
            var publicKey = _wrapper.GetPublicKeyFromPrivateKey(privateKey);
            publicKey.Bytes.Length.Should().Be(_wrapper.PublicKeyLength);
        }

        [Test]
        public void TestStdSignVerify()
        {
            var privateKey = _wrapper.GeneratePrivateKey();
            var message = Encoding.UTF8.GetBytes("fa la la la");
            var context = Encoding.UTF8.GetBytes("context");
            var signature = _wrapper.Sign(privateKey, message, context);
            var isVerified = _wrapper.Verify(signature, message, context);
            isVerified.Should().BeTrue();
        }

        [Test]
        public void TestLogicalFailureStdSignVerify()
        {
            var privateKey = _wrapper.GeneratePrivateKey();
            var message1 = Encoding.UTF8.GetBytes("fa la la la");
            var message2 = Encoding.UTF8.GetBytes("fa la la lahhhhhhh");
            var context = Encoding.UTF8.GetBytes("any old context");
            var signature = _wrapper.Sign(privateKey, message1, context);
            var isVerified = _wrapper.Verify(signature, message2, context);
            isVerified.Should().BeFalse();
        }

        [Test]
        public void TestSigningForMessagesMethodEquivalence()
        {
            var privateKey = _wrapper.GeneratePrivateKey();

            var message1 = new PublicEntry {Nonce = 123};
            var message2 = new PublicEntry {Nonce = 34534908};

            var expected = _wrapper.Sign(privateKey, message1.ToByteArray(), message2.ToByteArray());
            var actual = _wrapper.Sign(privateKey, message1, message2);

            actual.SignatureBytes.Should().BeEquivalentTo(expected.SignatureBytes);
            actual.PublicKeyBytes.Should().BeEquivalentTo(expected.PublicKeyBytes);
        }

        //From https://tools.ietf.org/html/rfc8032#section-7.3
        [Theory]
        [TestCase("616263",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406",
            "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", true)]
        [TestCase("616263",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406",
            "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "a", false)]
        [TestCase("616261",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406",
            "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", false)]
        [TestCase("616263",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083406",
            "0f1d1274943b91415889152e893d80e93275a1fc0b65fd71b4b0dda10ad7d772", "", false)]
        [TestCase("616263",
            "98a70222f0b8121aa9d30f813d683f809e462b469c7ff87639499bb94e6dae4131f85042463c2a355a2003d062adf5aaa10b8c61e636062aaad11c2a26083405",
            "ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf", "", false)]
        public void Ed25519PhTestVectors(string message,
            string signature,
            string publicKey,
            string context,
            bool expectedResult)
        {
            var signatureAndMessage = signature.HexToByteArray();
            var signatureBytes = new ArraySegment<byte>(signatureAndMessage, 0, _wrapper.SignatureLength).ToArray();
            var publicKeyBytes = publicKey.HexToByteArray();
            var messageBytes = message.HexToByteArray();
            var contextBytes = context.HexToByteArray();

            var sig = _wrapper.GetSignatureFromBytes(signatureBytes, publicKeyBytes);

            var isVerified = _wrapper.Verify(sig, messageBytes, contextBytes);
            isVerified.Should().Be(expectedResult);
        }

        [Theory]
        [TestCase("mL9Z+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIA==",
            "fa la la la")]
        public void TestFailureInvalidSignatureFormat(string sig, string msg)
        {
            var privateKey = _wrapper.GeneratePrivateKey();
            var publicKeyBytes = _wrapper.GetPublicKeyFromPrivateKey(privateKey).Bytes;
            var signatureBytes = Convert.FromBase64String(sig);
            var invalidSig = _wrapper.GetSignatureFromBytes(signatureBytes, publicKeyBytes);

            Action action = () => { _wrapper.Verify(invalidSig, Encoding.UTF8.GetBytes(msg), "".HexToByteArray()); };

            action.Should().Throw<SignatureException>();
        }

        [Test]
        public void TestVerifyingForMessagesMethodEquivalence()
        {
            var privateKey = _wrapper.GeneratePrivateKey();

            var message1 = new PublicEntry {Nonce = 123};
            var context = new SigningContext {NetworkType = NetworkType.Mainnet};

            var signature = _wrapper.Sign(privateKey, message1.ToByteArray(), context.ToByteArray());

            var expected = _wrapper.Verify(signature, message1.ToByteArray(), context.ToByteArray());
            var actual = _wrapper.Verify(signature, message1, context);

            actual.Should().Be(expected);
        }

        [Test]
        public void Is_PrivateKey_Length_Positive() { _wrapper.PrivateKeyLength.Should().BePositive(); }

        [Test]
        public void Is_PublicKey_Length_Positive() { _wrapper.PublicKeyLength.Should().BePositive(); }

        [Test]
        public void Is_Signature_Length_Positive() { _wrapper.SignatureLength.Should().BePositive(); }

        [Test]
        public void Is_Signature_Context_Max_Length_Positive()
        {
            _wrapper.SignatureContextMaxLength.Should().BePositive();
        }

        [Test]
        public void PublicKey_Can_Be_Created_With_Valid_Input()
        {
            const string publicKeyHex = "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025";
            var publicKeyBytes = publicKeyHex.HexToByteArray();
            var publicKey = _wrapper.GetPublicKeyFromBytes(publicKeyBytes);
            publicKey.Should().NotBe(null);
        }

        [Test]
        public void PublicKeyFromBytes_Throws_SignatureException_On_Invalid_Point()
        {
            const string publicKeyHex = "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908024";
            var publicKeyBytes = publicKeyHex.HexToByteArray();
            Action action = () => { _wrapper.GetPublicKeyFromBytes(publicKeyBytes); };
            action.Should().Throw<SignatureException>();
        }

        [Test]
        public void PublicKeyFromBytes_Throws_ArgumentException_On_Invalid_Length_PublicKey()
        {
            var publicKeyBytes = GenerateRandomByteArray(_wrapper.PublicKeyLength - 1);
            Action action = () => { _wrapper.GetPublicKeyFromBytes(publicKeyBytes); };
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void PrivateKey_Can_Be_Created_With_Valid_Input()
        {
            var privateKeyBytes = GenerateRandomByteArray(_wrapper.PrivateKeyLength);
            var privateKey = _wrapper.GetPrivateKeyFromBytes(privateKeyBytes);
            privateKey.Should().NotBe(null);
        }

        [Test]
        public void PrivateKeyFromBytes_Throws_ArgumentException_On_Invalid_Length_PrivateKey()
        {
            var privateKeyBytes = GenerateRandomByteArray(_wrapper.PrivateKeyLength + 1);
            Action action = () => { _wrapper.GetPrivateKeyFromBytes(privateKeyBytes); };
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SignatureFromBytes_Throws_ArgumentException_On_Invalid_PublicKey_Length()
        {
            var signatureBytes = GenerateRandomByteArray(_wrapper.SignatureLength);
            var publicKeyBytes = GenerateRandomByteArray(_wrapper.PrivateKeyLength + 1);

            Action action = () => { _wrapper.GetSignatureFromBytes(signatureBytes, publicKeyBytes); };

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SignatureFromBytes_Throws_ArgumentException_On_Invalid_Signature_Length()
        {
            var signatureBytes = GenerateRandomByteArray(_wrapper.SignatureLength - 1);

            var privateKey = _wrapper.GeneratePrivateKey();
            var publicKey = _wrapper.GetPublicKeyFromPrivateKey(privateKey);

            Action action = () => { _wrapper.GetSignatureFromBytes(signatureBytes, publicKey.Bytes); };

            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void Can_Create_Signature_With_Valid_Input()
        {
            var signatureBytes = GenerateRandomByteArray(_wrapper.SignatureLength);

            var privateKey = _wrapper.GeneratePrivateKey();
            var publicKey = _wrapper.GetPublicKeyFromPrivateKey(privateKey);

            _wrapper.GetSignatureFromBytes(signatureBytes, publicKey.Bytes).Should().NotBe(null);
        }

        [Test]
        public void Signature_Should_Contain_Public_Key_Corresponding_To_Private_Key()
        {
            var privateKey = _wrapper.GeneratePrivateKey();
            var publicKey = _wrapper.GetPublicKeyFromPrivateKey(privateKey);
            var message = Encoding.UTF8.GetBytes("fa la la la");
            var context = Encoding.UTF8.GetBytes("context");
            var signature = _wrapper.Sign(privateKey, message, context);
            signature.PublicKeyBytes.Should().Equal(publicKey.Bytes);
        }

        [Test]
        public void Batch_Verification_Passes_For_Valid_Batch()
        {
            var sigs = new List<ISignature>();
            var context = Encoding.UTF8.GetBytes("context");
            var messages = new List<byte[]> 
            {
                Encoding.UTF8.GetBytes("rat"), 
                Encoding.UTF8.GetBytes("hat"), 
                Encoding.UTF8.GetBytes("hut"),
                Encoding.UTF8.GetBytes("but"),
                Encoding.UTF8.GetBytes("bun"),
                Encoding.UTF8.GetBytes("run"),
                Encoding.UTF8.GetBytes("ran"),
                Encoding.UTF8.GetBytes("can"),
                Encoding.UTF8.GetBytes("cat"),
                Encoding.UTF8.GetBytes("rat")
            };
            messages.ForEach(x =>
            {
                sigs.Add(_wrapper.Sign(_wrapper.GeneratePrivateKey(), x, context));
            });

            var isVerified = _wrapper.BatchVerify(sigs, messages, context);
            isVerified.Should().BeTrue();
        }

        [Test]
        public void Batch_Verification_Fails_If_Wrong_Message_In_Batch()
        {
            var sigs = new List<ISignature>();
            var context = Encoding.UTF8.GetBytes("context");
            var messages = new List<byte[]> 
            {
                Encoding.UTF8.GetBytes("abc"), 
                Encoding.UTF8.GetBytes("123"), 
                Encoding.UTF8.GetBytes("xyz"),
            };
            messages.ForEach(x => sigs.Add(_wrapper.Sign(_wrapper.GeneratePrivateKey(), Encoding.UTF8.GetBytes("change of message"), context)));

            var isVerified = _wrapper.BatchVerify(sigs, messages, context);
            isVerified.Should().BeFalse();
        }

        [Test]
        public void Batch_Verification_Fails_If_Wrong_Context_In_Batch()
        {
            var sigs = new List<ISignature>();
            var context = Encoding.UTF8.GetBytes("context");
            var context2 = Encoding.UTF8.GetBytes("this context is different");
            var messages = new List<byte[]> 
            {
                Encoding.UTF8.GetBytes("abc"), 
                Encoding.UTF8.GetBytes("123"), 
                Encoding.UTF8.GetBytes("xyz"),
            };
            messages.ForEach(x => sigs.Add(_wrapper.Sign(_wrapper.GeneratePrivateKey(), x, context)));

            var isVerified = _wrapper.BatchVerify(sigs, messages, context2);
            isVerified.Should().BeFalse();
        }

        [Test]
        public void Batch_Verification_Fails_For_One_Incorrect_Signature_PublicKey_Pair_In_Batch()
        {
            var sigs = new List<ISignature>();
            var context = Encoding.UTF8.GetBytes("context");
            var messages = new List<byte[]> 
            {
                Encoding.UTF8.GetBytes("abc"), 
                Encoding.UTF8.GetBytes("123"), 
                Encoding.UTF8.GetBytes("xyz"),
            };
            messages.ForEach(x => sigs.Add(_wrapper.Sign(_wrapper.GeneratePrivateKey(), x, context)));
            sigs[1] = _wrapper.GetSignatureFromBytes(sigs[1].SignatureBytes, sigs[2].PublicKeyBytes);

            var isVerified = _wrapper.BatchVerify(sigs, messages, context);
            isVerified.Should().BeFalse();
        }

        [Test]
        public void Batch_Verification_Passes_For_Batch_Size_N()
        {
            const int N = 100;
            var messages = new List<byte[]>();
            var signatures = new List<ISignature>();
            var context = Encoding.UTF8.GetBytes("context");
            for (var i = 0; i < N; i++)
            {
                var bytes = new byte[255];
                Random.NextBytes(bytes);
                messages.Add(bytes);
            }

            messages.ForEach(x =>
            {
                signatures.Add(_wrapper.Sign(_wrapper.GeneratePrivateKey(), x, context));
            });
            
            var isVerified = _wrapper.BatchVerify(signatures, messages, context);
            isVerified.Should().BeTrue();
        }

        private static byte[] GenerateRandomByteArray(int length)
        {
            var buf = new byte[length];
            Random.NextBytes(buf);
            return buf;
        }
    }
}
