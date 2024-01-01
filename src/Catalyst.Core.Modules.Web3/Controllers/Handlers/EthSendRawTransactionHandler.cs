#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.IO;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Crypto;
using Nethermind.Evm.Tracing.GethStyle.JavaScript;
using Nethermind.Logging;
using Nethermind.Serialization.Rlp;
using Nethermind.Specs;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "sendRawTransaction")]
    public class EthSendRawTransactionHandler : EthWeb3RequestHandler<byte[], Hash256>
    {
        protected override Hash256 Handle(byte[] transaction, IWeb3EthApi api)
        {
            PublicEntry publicEntry;
            try
            {
                Transaction tx = Rlp.Decode<Transaction>(transaction);
                EthereumEcdsa ecdsa = new EthereumEcdsa(MainnetSpecProvider.Instance.ChainId, LimboLogs.Instance);
                tx.SenderAddress = ecdsa.RecoverAddress(tx, false);
                tx.Timestamp = (Nethermind.Int256.UInt256) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                publicEntry = new PublicEntry
                {
                    Data = ByteString.CopyFrom(tx.Data.ToBytes()),
                    GasLimit = (ulong) tx.GasLimit,
                    GasPrice = ByteString.CopyFrom(tx.GasPrice.ToBytes()),
                    Nonce = (ulong) tx.Nonce,
                    SenderAddress = tx.SenderAddress.Bytes.ToByteString(),
                    ReceiverAddress = tx.To?.Bytes.ToByteString() ?? ByteString.Empty,
                    Amount = ByteString.CopyFrom(tx.Value.ToBytes()),
                    Signature = new Protocol.Cryptography.Signature
                    {
                        RawBytes = ByteString.CopyFrom((byte) 1)
                    }
                };
            }
            catch
            {
                try
                {
                    TransactionBroadcast transactionBroadcast = TransactionBroadcast.Parser.ParseFrom(transaction);
                    publicEntry = transactionBroadcast.PublicEntry;
                }
                catch (Exception)
                {
                    throw new InvalidDataException($"Transaction data could not be deserialized into a {nameof(PublicEntry)}");
                }
            }

            return api.SendTransaction(publicEntry);
        }
    }
}
