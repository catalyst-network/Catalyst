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

namespace Catalyst.Core.Lib.Tests.UnitTests.Validators
{
    public sealed class TransactionValidatorTests
    {
        [Test]
        public void TransactionValidator_ValidateTransactionSignature_returns_false_when_signature_is_null()
        {
            var subbedLogger = Substitute.For<ILogger>();
            var subbedContext = Substitute.For<ICryptoContext>();

            var transactionValidator = new TransactionValidator(subbedLogger, subbedContext);

            var invalidSignature = new Signature();
            var invalidTransaction = new PublicEntry {Signature = invalidSignature};

            var result = transactionValidator.ValidateTransaction(invalidTransaction);
            result.Should().BeFalse();
        }

        [Test]
        public void
            TransactionValidator_ValidateTransactionSignature_returns_true_for_valid_transaction_signature_verification()
        {
            var subbedLogger = Substitute.For<ILogger>();
            var signatureResult = Substitute.For<ISignature>();
            var subbedContext = new FakeContext(signatureResult, true);

            signatureResult.SignatureBytes.Returns(new byte[64]);

            var transactionValidator = new TransactionValidator(subbedLogger, subbedContext);
            var privateKey = Substitute.For<IPrivateKey>();

            var validTransaction = new PublicEntry
            {
                SenderAddress = privateKey.GetPublicKey().Bytes.ToByteString()
            };

            var signature = new Signature
            {
                // sign an actual TransactionBroadcast object
                RawBytes = subbedContext.Sign(privateKey, validTransaction, new SigningContext())
                   .SignatureBytes.ToByteString(),
                SigningContext = new SigningContext
                {
                    NetworkType = NetworkType.Devnet,
                    SignatureType = SignatureType.TransactionPublic
                }
            };

            validTransaction.Signature = signature;

            var result = transactionValidator.ValidateTransaction(validTransaction);
            result.Should().BeTrue();
        }

        [Test]
        public void
            TransactionValidator_ValidateTransactionSignature_returns_false_for_invalid_transaction_signature_verification()
        {
            var subbedLogger = Substitute.For<ILogger>();
            var signatureResult = Substitute.For<ISignature>();
            var subbedContext = new FakeContext(signatureResult, false);

            signatureResult.SignatureBytes.Returns(new byte[64]);

            var privateKey = Substitute.For<IPrivateKey>();

            // raw un-signed tx message
            var validTransaction = new PublicEntry
            {
                SenderAddress = privateKey.GetPublicKey().Bytes.ToByteString()
            };

            var txSig = new Signature
            {
                RawBytes = new byte[64]
                   .ToByteString(), //random bytes that are not of a signed TransactionBroadcast Object
                SigningContext = new SigningContext
                {
                    NetworkType = NetworkType.Devnet,
                    SignatureType = SignatureType.TransactionPublic
                }
            };

            var transactionValidator = new TransactionValidator(subbedLogger, subbedContext);

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
        }
    }
}
