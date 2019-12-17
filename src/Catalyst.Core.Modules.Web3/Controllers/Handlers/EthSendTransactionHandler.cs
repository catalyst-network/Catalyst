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

using Catalyst.Abstractions.Kvm.Models;
using Catalyst.Abstractions.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Transaction;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "sendTransaction")]
    public class EthSendTransactionHandler : EthWeb3RequestHandler<TransactionForRpc, Keccak>
    {
        protected override Keccak Handle(TransactionForRpc transaction, IWeb3EthApi api)
        {
            var tx = transaction;

            PublicEntry publicEntry = new PublicEntry
            {
                //Data = tx.Data.ToByteString(),
                //Nonce = (ulong)tx.Nonce,
                //SenderAddress = tx.SenderAddress.Bytes.ToByteString(),
                //ReceiverAddress = tx.To.Bytes.ToByteString(),
                //Amount = tx.Value.ToUint256ByteString(),
                //Timestamp = new Timestamp { Seconds = (long)tx.t },
            };

            if (tx.GasPrice != null)
            {
                publicEntry.GasPrice = new UInt256(tx.GasPrice.Value).ToUint256ByteString();
            }

            return api.SendTransaction(publicEntry);
        }
    }
}
