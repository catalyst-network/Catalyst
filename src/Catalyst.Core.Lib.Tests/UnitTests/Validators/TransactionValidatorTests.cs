#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Validators;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Core.Lib.Config;
using Catalyst.Abstractions.Config;

namespace Catalyst.Core.Lib.Tests.UnitTests.Validators
{
    [TestFixture]
    public sealed class TransactionValidatorTests
    {
        private ICryptoContext _cryptoContext;
        private ITransactionConfig _transactionConfig;
        private ILogger _logger;
        private SigningContext _signingContext;

        [SetUp]
        public void Init()
        {
            _logger = Substitute.For<ILogger>();
            _cryptoContext = Substitute.For<ICryptoContext>();
            _transactionConfig = new TransactionConfig();
            _signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };
        }

        [Test]
        public void TransactionValidator_ValidateTransactionSignature_Returns_False_When_Signature_Is_Null()
        {
            var invalidSignature = new Signature();
            var invalidTransaction = new PublicEntry { Signature = invalidSignature, GasLimit = _transactionConfig.MinTransactionEntryGasLimit };
            var transactionValidator = new TransactionValidator(_cryptoContext, _transactionConfig, _logger);

            var result = transactionValidator.ValidateTransaction(invalidTransaction);
            result.Should().BeFalse();
        }

        [Test]
        public void TransactionValidator_ValidateTransactionGasLimit_Returns_False_When_Gas_Is_Under_Minimum()
        {
            var signatureResult = Substitute.For<ISignature>();
            var subbedContext = new FakeContext(signatureResult, true);

            signatureResult.SignatureBytes.Returns(new byte[64]);

            var transactionValidator = new TransactionValidator(subbedContext, _transactionConfig, _logger);
            var privateKey = Substitute.For<IPrivateKey>();

            var invalidTransaction = new PublicEntry
            {
                SenderAddress = privateKey.GetPublicKey().Bytes.ToByteString(),
                //Set gas limit to one less then minimum
                GasLimit = _transactionConfig.MinTransactionEntryGasLimit-1
            };

            var signature = new Signature
            {
                //Sign an actual TransactionBroadcast object
                RawBytes = subbedContext.Sign(privateKey, invalidTransaction, new SigningContext())
                   .SignatureBytes.ToByteString(),
                SigningContext = _signingContext
            };

            invalidTransaction.Signature = signature;

            var result = transactionValidator.ValidateTransaction(invalidTransaction);
            result.Should().BeFalse();
        }

        [Test]
        public void
            TransactionValidator_ValidateTransactionSignature_Returns_True_For_Valid_Transaction_Signature_Verification()
        {
            var signatureResult = Substitute.For<ISignature>();
            var subbedContext = new FakeContext(signatureResult, true);

            signatureResult.SignatureBytes.Returns(new byte[64]);

            var transactionValidator = new TransactionValidator(subbedContext, _transactionConfig, _logger);
            var privateKey = Substitute.For<IPrivateKey>();

            var validTransaction = new PublicEntry
            {
                SenderAddress = privateKey.GetPublicKey().Bytes.ToByteString(),
                GasLimit = _transactionConfig.MinTransactionEntryGasLimit
            };

            var signature = new Signature
            {
                //Sign an actual TransactionBroadcast object
                RawBytes = subbedContext.Sign(privateKey, validTransaction, new SigningContext())
                   .SignatureBytes.ToByteString(),
                SigningContext = _signingContext
            };

            validTransaction.Signature = signature;

            var result = transactionValidator.ValidateTransaction(validTransaction);
            result.Should().BeTrue();
        }

        [Test]
        public void
            TransactionValidator_ValidateTransactionSignature_Returns_False_For_Invalid_Transaction_Signature_Verification()
        {
            var signatureResult = Substitute.For<ISignature>();
            var subbedContext = new FakeContext(signatureResult, false);

            signatureResult.SignatureBytes.Returns(new byte[64]);

            var privateKey = Substitute.For<IPrivateKey>();

            // raw un-signed tx message
            var validTransaction = new PublicEntry
            {
                SenderAddress = privateKey.GetPublicKey().Bytes.ToByteString(),
                GasLimit = _transactionConfig.MinTransactionEntryGasLimit
            };

            var txSig = new Signature
            {
                RawBytes = new byte[64]
                   .ToByteString(), //random bytes that are not of a signed TransactionBroadcast Object
                SigningContext = _signingContext
            };

            var transactionValidator = new TransactionValidator(subbedContext, _transactionConfig, _logger);

            validTransaction.Signature = txSig;

            var result = transactionValidator.ValidateTransaction(validTransaction);
            result.Should().BeFalse();
        }

        private sealed class FakeContext : ICryptoContext
        {
            private readonly ISignature _signature;
            private readonly bool _verifyResult;

            public FakeContext(ISignature signature, bool verifyResult)
            {
                _signature = signature;
                _verifyResult = verifyResult;
            }

            public int PrivateKeyLength { get; }
            public int PublicKeyLength { get; }
            public int SignatureLength { get; }
            public int SignatureContextMaxLength { get; }
            public IPrivateKey GeneratePrivateKey() { throw new NotImplementedException(); }

            public IPublicKey GetPublicKeyFromPrivateKey(IPrivateKey privateKey)
            {
                throw new NotImplementedException();
            }

            public IPublicKey GetPublicKeyFromBytes(byte[] publicKeyBytes) { throw new NotImplementedException(); }
            public IPrivateKey GetPrivateKeyFromBytes(byte[] privateKeyBytes) { throw new NotImplementedException(); }
            public byte[] ExportPrivateKey(IPrivateKey privateKey) { throw new NotImplementedException(); }
            public byte[] ExportPublicKey(IPublicKey publicKey) { throw new NotImplementedException(); }

            public ISignature Sign(IPrivateKey privateKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> context)
            {
                return _signature;
            }

            public ISignature GetSignatureFromBytes(byte[] signatureBytes, byte[] publicKeyBytes)
            {
                return Substitute.For<ISignature>();
            }

            public bool Verify(ISignature signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> context)
            {
                return _verifyResult;
            }

            public bool BatchVerify(IList<ISignature> signatures, IList<byte[]> messages, ReadOnlySpan<byte> context) { throw new NotImplementedException(); }
        }
    }
}
