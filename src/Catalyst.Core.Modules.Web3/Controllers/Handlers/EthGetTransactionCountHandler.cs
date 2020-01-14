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
using Catalyst.Abstractions.Kvm.Models;
using Catalyst.Abstractions.Ledger;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "getTransactionCount")]
    public class EthGetTransactionsCountHandler : EthWeb3RequestHandler<Address, BlockParameter, UInt256>
    {
        protected override UInt256 Handle(Address address, BlockParameter block, IWeb3EthApi api)
        {
            if (api.TryGetDeltaWithCid(block, out var deltaWithCid))
            {
                Keccak stateRoot = deltaWithCid.Delta.StateRootAsKeccak();
                Account account = api.StateReader.GetAccount(stateRoot, address);
                return account?.Nonce ?? 0;
            }

            throw new InvalidOperationException($"Delta not found: '{block}'");
        }
    }

    [EthWeb3RequestHandler("eth", "getBlockTransactionCountByHash")]
    public class EthGetBlockTransactionCountByHash : EthWeb3RequestHandler<Keccak, UInt256?>
    {
        protected override UInt256? Handle(Keccak txHash, IWeb3EthApi api)
        {
            throw new NotImplementedException();
        }
    }
    
    [EthWeb3RequestHandler("eth", "getBlockTransactionCountByNumber")]
    public class EthGetBlockTransactionCountByNumber : EthWeb3RequestHandler<BlockParameter, UInt256?>
    {
        protected override UInt256? Handle(BlockParameter txHash, IWeb3EthApi api)
        {
            throw new NotImplementedException();
        }
    }

    [EthWeb3RequestHandler("eth", "getTransactionByHash")]
    public class EthGetTransactionsByHashHandler : EthWeb3RequestHandler<Keccak, TransactionForRpc>
    {
        protected override TransactionForRpc Handle(Keccak txHash, IWeb3EthApi api)
        {
            throw new NotImplementedException();
        }
    }

    [EthWeb3RequestHandler("eth", "getTransactionByBlockHashAndIndex")]
    public class EthGetTransactionByBlockHashAndIndex : EthWeb3RequestHandler<Keccak, UInt256, TransactionForRpc>
    {
        protected override TransactionForRpc Handle(Keccak blockHash, UInt256 positionIndex, IWeb3EthApi api)
        {
            throw new NotImplementedException();
        }
    }

    [EthWeb3RequestHandler("eth", "getTransactionByBlockNumberAndIndex")]
    public class EthGetTransactionByBlockNumberAndIndex : EthWeb3RequestHandler<BlockParameter, UInt256, TransactionForRpc>
    {
        protected override TransactionForRpc Handle(BlockParameter blockParameter, UInt256 positionIndex, IWeb3EthApi api)
        {
            throw new NotImplementedException();
        }
    }

    [EthWeb3RequestHandler("eth", "getUncleByBlockHashAndIndex")]
    public class EthGetUncleByBlockHashAndIndex : EthWeb3RequestHandler<Keccak, UInt256, BlockForRpc>
    {
        protected override BlockForRpc Handle(Keccak blockHashData, UInt256 positionIndex, IWeb3EthApi api) => null;
    }

    [EthWeb3RequestHandler("eth", "getUncleByBlockNumberAndIndex")]
    public class EthGetUncleByBlockNumberAndIndex : EthWeb3RequestHandler<BlockParameter, UInt256, BlockForRpc>
    {
        protected override BlockForRpc Handle(BlockParameter blockParameter, UInt256 positionIndex, IWeb3EthApi api) => null;
    }

    [EthWeb3RequestHandler("eth", "getUncleCountByBlockHash")]
    public class EthGetUncleCountByBlockHash : EthWeb3RequestHandler<UInt256?>
    {
        protected override UInt256? Handle(IWeb3EthApi api) => UInt256.Zero;
    }
    
    [EthWeb3RequestHandler("eth", "getUncleCountByBlockNumber")]
    public class EthGetUncleCountByBlockNumber : EthWeb3RequestHandler<UInt256?>
    {
        protected override UInt256? Handle(IWeb3EthApi api) => UInt256.Zero;
    }
}
