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

using Catalyst.Abstractions.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Transaction;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Encoding;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "sendRawTransaction")]
    public class EthSendRawTransactionHandler : EthWeb3RequestHandler<byte[], Keccak>
    {
        protected override Keccak Handle(byte[] transaction, IWeb3EthApi api)
        {
            Transaction tx = Rlp.Decode<Transaction>(transaction);

            PublicEntry publicEntry = new PublicEntry
            {
                Data = tx.Data.ToByteString(),
                GasLimit = (ulong) tx.GasLimit,
                GasPrice = tx.GasPrice.ToUint256ByteString(),
                Nonce = (ulong) tx.Nonce,
                SenderAddress = tx.SenderAddress.Bytes.ToByteString(),
                ReceiverAddress = tx.To.Bytes.ToByteString(),
                Amount = tx.Value.ToUint256ByteString(),
                Timestamp = new Timestamp {Seconds = (long) tx.Timestamp},
            };

            return api.SendTransaction(publicEntry);
        }
    }
}
