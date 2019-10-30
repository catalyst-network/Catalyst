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
using Catalyst.Protocol.Wire;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.Validators
{
    public sealed class TransactionValidatorTests
    {
        [Fact]
        public void ValidateTransactionSignature_will_pass_with_valid_transaction_signature()
        {
            var subbedLogger = Substitute.For<ILogger>();
            var cryptoContext = new FfiWrapper();
            var transactionValidator = new TransactionValidator(subbedLogger, cryptoContext);
            
            // build a valid transaction
            var privateKey = cryptoContext.GeneratePrivateKey();

            var validTransactionBroadcast = new TransactionBroadcast
            {
                PublicEntries =
                {
                    new PublicEntry
                    {
                        Base = new BaseEntry
                        {
                            SenderPublicKey = privateKey.GetPublicKey().Bytes.ToByteString()
                        }
                    }
                }
            };

            var signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };

            var signature = new Signature
            {
                // sign an actual TransactionBroadcast object
                RawBytes = cryptoContext.Sign(privateKey, validTransactionBroadcast.ToByteArray(), signingContext.ToByteArray())
                   .SignatureBytes.ToByteString(),
                SigningContext = signingContext
            };
            
            validTransactionBroadcast.Signature = signature;

            var result = transactionValidator.ValidateTransaction(validTransactionBroadcast);
            result.Should().BeTrue();
        }
    }
}
