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
using Catalyst.Abstractions.Repository;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Transaction;
using Nethermind.Dirichlet.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers 
{
    [EthWeb3RequestHandler("eth", "pendingTransactions")]
    public class EthPendingTransactionsHandler : EthWeb3RequestHandler<IEnumerable<TransactionForRpc>>
    {
        protected override IEnumerable<TransactionForRpc> Handle(IWeb3EthApi api)
        {
            var transactions = api.GetPendingTransactions();
            return ConvertMempoolTransactions(api, transactions.ToList());
        }

        private IEnumerable<TransactionForRpc> ConvertMempoolTransactions(IWeb3EthApi api, IList<PublicEntry> publicEntries)
        {
            foreach (var publicEntry in publicEntries)
            {
                yield return new TransactionForRpc
                {
                    GasPrice = publicEntry.GasPrice.ToUInt256(),
                    BlockHash = null,
                    BlockNumber = (UInt256)0x0,
                    Nonce = publicEntry.Nonce,
                    To = Web3EthApiExtensions.ToAddress(publicEntry.ReceiverAddress),
                    From = Web3EthApiExtensions.ToAddress(publicEntry.SenderAddress),
                    Value = publicEntry.Amount.ToUInt256(),
                    Hash = publicEntry.GetHash(api.HashProvider),
                    Data = publicEntry.Data.ToByteArray(),
                    R = new byte[0],
                    S = new byte[0],
                    V = UInt256.Zero,
                    Gas = publicEntry.GasLimit,
                    TransactionIndex = (UInt256)0x0
                };
            }
        }
    }
}
