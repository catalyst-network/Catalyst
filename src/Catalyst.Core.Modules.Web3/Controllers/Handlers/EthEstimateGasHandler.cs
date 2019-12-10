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
using Catalyst.Protocol.Deltas;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm.Tracing;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "estimateGas")]
    public class EthEstimateGasHandler : EthWeb3RequestHandler<TransactionForRpc, long>
    {
        protected override long Handle(TransactionForRpc transactionCall, IWeb3EthApi api)
        {
            long deltaNumber = api.DeltaResolver.LatestDeltaNumber;
            Delta delta = api.GetLatestDelta();
            Keccak root = new Keccak(delta.StateRoot);

            if (transactionCall.Gas == null)
            {
                transactionCall.Gas = delta.GasLimit;
            }

            Transaction transaction = transactionCall.ToTransaction();

            transaction.Nonce = api.StateReader.GetNonce(root, transaction.SenderAddress);
            transaction.Hash = Transaction.CalculateHash(transaction);

            CallOutputTracer callOutputTracer = new CallOutputTracer();
            BlockHeader header = new BlockHeader(Keccak.Zero, Keccak.Zero, Address.Zero, 1, deltaNumber, (long) delta.GasLimit, new UInt256(delta.TimeStamp.Seconds), null);

            lock (api.SyncRoot)
            {
                api.StateProvider.StateRoot = root;

                api.Processor.CallAndRestore(transaction, header, callOutputTracer);
                api.StateProvider.Reset();
                api.StorageProvider.Reset();
            }

            return callOutputTracer.GasSpent;
        }
    }
}
