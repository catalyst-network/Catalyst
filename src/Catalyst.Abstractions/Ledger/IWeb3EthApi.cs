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

using System;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Kvm;
using Catalyst.Protocol.Deltas;
using LibP2P;
using Nethermind.Store;
using Nethermind.Evm;

namespace Catalyst.Abstractions.Ledger
{
    public interface IWeb3EthApi
    {
        IDeltaExecutor DeltaExecutor { get; }
        IStateReader StateReader { get; }
        IDeltaResolver DeltaResolver { get; }
        IStateRootResolver StateRootResolver { get; }
        IDeltaCache DeltaCache { get; }

        object SyncRoot { get; }
        ITransactionProcessor Processor { get; }
        IStorageProvider StorageProvider { get; }
        IStateProvider StateProvider { get; }
    }

    public static class Web3EthApiExtensions
    {
        public static Delta GetLatestDelta(this IWeb3EthApi api)
        {
            // change to appropriate hash
            Cid cid = api.DeltaResolver.LatestDelta;
            if (!api.DeltaCache.TryGetOrAddConfirmedDelta(cid, out Delta delta))
            {
                throw new Exception($"Delta not found '{cid}'");
            }

            return delta;
        }
    }
}
