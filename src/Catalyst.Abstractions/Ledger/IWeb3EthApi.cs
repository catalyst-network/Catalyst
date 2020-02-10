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

using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Lib.P2P;
using Nethermind.Core.Crypto;
using Nethermind.Store;

namespace Catalyst.Abstractions.Ledger
{
    public interface IWeb3EthApi
    {
        IStateReader StateReader { get; }
        IDeltaResolver DeltaResolver { get; }
        IDeltaCache DeltaCache { get; }

        IDeltaExecutor Executor { get; }
        IStorageProvider StorageProvider { get; }
        IStateProvider StateProvider { get; }
        IHashProvider HashProvider { get; }

        Keccak SendTransaction(PublicEntry publicEntry);
        
        TransactionReceipt FindReceipt(Keccak transactionHash);
        bool FindTransactionData(Keccak transactionHash, out Cid deltaHash, out Delta delta, out int index);
    }
}
