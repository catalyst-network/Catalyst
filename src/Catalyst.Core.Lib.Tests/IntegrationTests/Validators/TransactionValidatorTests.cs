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

using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Validators;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Core.Lib.Config;
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Validators;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.Validators
{
    [TestFixture]
    public sealed class TransactionValidatorTests
    {
        private ITransactionConfig _transactionConfig;
        private ICryptoContext _cryptoContext;
        private ITransactionValidator _transactionValidator;
        private ILogger _logger;

        private IPrivateKey _privateKey;

        private SigningContext _signingContext;

        [SetUp]
        public void Init()
        {
            _transactionConfig = new TransactionConfig();
            _cryptoContext = new FfiWrapper();
            _logger = Substitute.For<ILogger>();
            _transactionValidator = new TransactionValidator(_cryptoContext, new TransactionConfig(), _logger);
            _privateKey = _cryptoContext.GeneratePrivateKey();
            _signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };
        }

        [Test]
        public void ValidateTransactionSignature_Will_Pass_With_Valid_Transaction_Signature()
        {
            var validTransaction = new PublicEntry
            {
                SenderAddress = _privateKey.GetPublicKey().Bytes.ToByteString(),
                GasLimit = _transactionConfig.MinTransactionEntryGasLimit
            };

            var signature = new Signature
            {
                //Sign an actual PublicEntry object
                RawBytes = _cryptoContext.Sign(_privateKey, validTransaction.ToByteArray(), _signingContext.ToByteArray())
                   .SignatureBytes.ToByteString(),
                SigningContext = _signingContext
            };

            validTransaction.Signature = signature;

            var result = _transactionValidator.ValidateTransaction(validTransaction);
            result.Should().BeTrue();
        }

        [Test]
        public void ValidateTransactionSignature_Will_Fail_With_Invalid_Transaction_Signature()
        {
            var invalidTransaction = new PublicEntry
            {
                SenderAddress = _privateKey.GetPublicKey().Bytes.ToByteString(),
                GasLimit = _transactionConfig.MinTransactionEntryGasLimit
            };

            var signature = new Signature
            {
                //Sign an actual PublicEntry with a different private key to create a invalid signature
                RawBytes = _cryptoContext.Sign(_cryptoContext.GeneratePrivateKey(), invalidTransaction.ToByteArray(), _signingContext.ToByteArray())
                   .SignatureBytes.ToByteString(),
                SigningContext = _signingContext
            };

            invalidTransaction.Signature = signature;

            var result = _transactionValidator.ValidateTransaction(invalidTransaction);
            result.Should().BeFalse();
        }

        [Test]
        public void ValidateTransaction_Will_Fail_With_Less_Than_Minimum_Gas_Limit()
        {
            var invalidTransaction = new PublicEntry
            {
                SenderAddress = _privateKey.GetPublicKey().Bytes.ToByteString(),
                //Set GasLimit to 1 less than minimum
                GasLimit = _transactionConfig.MinTransactionEntryGasLimit - 1
            };

            var signature = new Signature
            {
                RawBytes = _cryptoContext.Sign(_privateKey, invalidTransaction.ToByteArray(), _signingContext.ToByteArray())
                   .SignatureBytes.ToByteString(),
                SigningContext = _signingContext
            };

            invalidTransaction.Signature = signature;

            var result = _transactionValidator.ValidateTransaction(invalidTransaction);
            result.Should().BeFalse();
        }
    }
}
