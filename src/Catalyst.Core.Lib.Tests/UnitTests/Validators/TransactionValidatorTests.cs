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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Validators;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Validators
{
    public sealed class TransactionValidatorTests
    {
        [Fact]
        public void TransactionValidator_ValidateTransactionSignature_returns_false_when_signature_is_null()
        {
            var subbedLogger = Substitute.For<ILogger>();
            var subbedContext = Substitute.For<ICryptoContext>();

            var transactionValidator = new TransactionValidator(subbedLogger, subbedContext);

            var invalidSignature = new Signature();
            var invalidTransactionBroadcast = new TransactionBroadcast
            {
                PublicEntry = new PublicEntry() { Signature = invalidSignature }
            };

            var result = transactionValidator.ValidateTransaction(invalidTransactionBroadcast);
            result.Should().BeFalse();
        }

        [Fact]
        public void
            TransactionValidator_ValidateTransactionSignature_returns_true_for_valid_transaction_signature_verification()
        {
            var subbedLogger = Substitute.For<ILogger>();
            var subbedContext = Substitute.For<ICryptoContext>();

            subbedContext.GetSignatureFromBytes(Arg.Any<byte[]>(), Arg.Any<byte[]>())
               .ReturnsForAnyArgs(Substitute.For<ISignature>());

            subbedContext.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
               .Returns(true);

            subbedContext.Sign(Arg.Any<IPrivateKey>(), Arg.Any<byte[]>(), Arg.Any<byte[]>())
               .SignatureBytes.Returns(new byte[64]);

            var transactionValidator = new TransactionValidator(subbedLogger, subbedContext);
            var privateKey = subbedContext.GeneratePrivateKey();

            var validTransactionBroadcast = new TransactionBroadcast
            {
                PublicEntry = new PublicEntry
                {
                    Base = new BaseEntry
                    {
                        SenderPublicKey = privateKey.GetPublicKey().Bytes.ToByteString()
                    }
                }
            };

            var signature = new Signature
            {
                // sign an actual TransactionBroadcast object
                RawBytes = subbedContext.Sign(privateKey, validTransactionBroadcast.ToByteArray(), Arg.Any<byte[]>())
                   .SignatureBytes.ToByteString(),
                SigningContext = new SigningContext
                {
                    NetworkType = NetworkType.Devnet,
                    SignatureType = SignatureType.TransactionPublic
                }
            };

            validTransactionBroadcast.PublicEntry.Signature = signature;

            var result = transactionValidator.ValidateTransaction(validTransactionBroadcast);
            result.Should().BeTrue();
        }

        [Fact]
        public void TransactionValidator_ValidateTransactionSignature_returns_false_for_invalid_transaction_signature_verification()
        {
            var subbedLogger = Substitute.For<ILogger>();
            var subbedContext = Substitute.For<ICryptoContext>();

            var privateKey = subbedContext.GeneratePrivateKey();

            // raw un-signed tx message
            var validTransactionBroadcast = new TransactionBroadcast
            {
                PublicEntry = new PublicEntry
                {
                    Base = new BaseEntry
                    {
                        SenderPublicKey = privateKey.GetPublicKey().Bytes.ToByteString()
                    }
                }
            };

            var txSig = new Signature
            {
                RawBytes = new byte[64].ToByteString(), //random bytes that are not of a signed TransactionBroadcast Object
                SigningContext = new SigningContext
                {
                    NetworkType = NetworkType.Devnet,
                    SignatureType = SignatureType.TransactionPublic
                }
            };

            subbedContext.GetSignatureFromBytes(Arg.Is(txSig.ToByteArray()), Arg.Is(privateKey.GetPublicKey().Bytes))
               .ReturnsForAnyArgs(Substitute.For<ISignature>());

            subbedContext.Verify(Arg.Any<ISignature>(), // @TODO be more specific
                    Arg.Is(validTransactionBroadcast.ToByteArray()),
                    Arg.Is(txSig.SigningContext.ToByteArray())
                )
               .Returns(false);

            subbedContext.Sign(Arg.Is(privateKey), validTransactionBroadcast.ToByteArray(), txSig.SigningContext.ToByteArray())
               .SignatureBytes.Returns(new byte[64]);

            var transactionValidator = new TransactionValidator(subbedLogger, subbedContext);

            validTransactionBroadcast.PublicEntry.Signature = txSig;

            var result = transactionValidator.ValidateTransaction(validTransactionBroadcast);
            result.Should().BeFalse();
        }
    }
}

