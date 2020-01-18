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

using System.Linq;
using Catalyst.Abstractions.Kvm.Models;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Ledger.Models;
using Nethermind.Core;
using Nethermind.Core.Crypto;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "getTransactionReceipt")]
    public class EthGetTransactionReceiptHandler : EthWeb3RequestHandler<Keccak, ReceiptForRpc>
    {
        protected override ReceiptForRpc Handle(Keccak txHash, IWeb3EthApi api)
        {
            TransactionReceipt receipt = api.FindReceipt(txHash);

            if (receipt == null)
            {
                return null;
            }

            var receiptForRpc = new ReceiptForRpc
            {
                TransactionHash = txHash,
                TransactionIndex = receipt.Index,
                BlockHash = receipt.DeltaHash,
                BlockNumber = receipt.DeltaNumber,
                CumulativeGasUsed = receipt.GasUsedTotal,
                GasUsed = receipt.GasUsed,
                From = receipt.Sender,
                To = receipt.Recipient,
                ContractAddress = receipt.ContractAddress,
                Logs = receipt.Logs.Select((l, idx) => new LogEntryForRpc(receipt, l, idx)).ToArray(),
                LogsBloom = new Bloom(receipt.Logs),
                Status = receipt.StatusCode,
            };

            return receiptForRpc;
        }
    }
}
