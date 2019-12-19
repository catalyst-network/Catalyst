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
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Nethermind.Core.Crypto;
using Nethermind.Evm.Tracing;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "estimateGas")]
    public class EthEstimateGasHandler : EthWeb3RequestHandler<TransactionForRpc, long>
    {
        protected override long Handle(TransactionForRpc transactionCall, IWeb3EthApi api)
        {
            Delta delta = api.GetLatestDelta();
            Keccak root = new Keccak(delta.StateRoot.ToByteArray());

            if (transactionCall.Gas == null)
            {
                transactionCall.Gas = delta.GasLimit;
            }

            Delta newDelta = delta.Clone();

            newDelta.PreviousDeltaDfsHash = api.DeltaResolver.LatestDelta.ToArray().ToByteString();
            newDelta.CoinbaseEntries.Clear();
            newDelta.ConfidentialEntries.Clear();
            newDelta.PublicEntries.Clear();
            newDelta.PublicEntries.Add(new PublicEntry
            {
                Nonce = (ulong) api.StateReader.GetNonce(root, transactionCall.From),
                
            });
            
            CallOutputTracer callOutputTracer = new CallOutputTracer();

            api.StateProvider.StateRoot = root;
            api.Executor.CallAndRestore(newDelta, callOutputTracer);
            api.StateProvider.Reset();
            api.StorageProvider.Reset();

            return callOutputTracer.GasSpent;
        }
    }
}
