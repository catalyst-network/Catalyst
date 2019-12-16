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
using Catalyst.Abstractions.Kvm.Models;
using Catalyst.Abstractions.Ledger;
using Catalyst.Protocol.Deltas;
using Google.Protobuf;
using LibP2P;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
using Address = Nethermind.Core.Address;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "getBlockByNumber")]
    public class EthGetBlockByNumberHandler : EthWeb3RequestHandler<BlockParameter, BlockForRpc>
    {
        protected override BlockForRpc Handle(BlockParameter block, IWeb3EthApi api)
        {
            Cid deltaHash;
            long blockNumber;

            IDeltaCache deltaCache = api.DeltaCache;
            IDeltaResolver deltaResolver = api.DeltaResolver;

            switch (block.Type)
            {
                case BlockParameterType.Earliest:
                    deltaHash = deltaCache.GenesisHash;
                    blockNumber = 0;
                    break;
                case BlockParameterType.Latest:
                    deltaHash = deltaResolver.LatestDelta;
                    blockNumber = deltaResolver.LatestDeltaNumber;
                    break;
                case BlockParameterType.Pending:
                    deltaHash = deltaResolver.LatestDelta;
                    blockNumber = deltaResolver.LatestDeltaNumber;
                    break;
                case BlockParameterType.BlockNumber:
                    blockNumber = block.BlockNumber.Value;
                    deltaHash = deltaResolver.Resolve(blockNumber);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Delta delta = api.GetDelta(deltaHash);
            return BuildBlock(delta, deltaHash, blockNumber);
        }

        private static BlockForRpc BuildBlock(Delta delta, Cid deltaHash, long blockNumber)
        {
            var hash0 = Keccak.Zero;

            BlockForRpc blockForRpc = new BlockForRpc
            {
                Miner = Address.Zero,
                Difficulty = 1,
                Hash = GetValue(deltaHash),
                Number = blockNumber,
                GasLimit = (long) delta.GasLimit,
                GasUsed = delta.GasUsed,
                Timestamp = new UInt256(delta.TimeStamp.Seconds),
                ParentHash = blockNumber == 0 ? hash0 : GetValue(delta.PreviousDeltaDfsHash),
                StateRoot = GetValue(delta.MerkleRoot),
                ReceiptsRoot = hash0,
                TransactionsRoot = hash0,
                LogsBloom = Bloom.Empty
            };
            blockForRpc.TotalDifficulty = (UInt256) ((long) blockForRpc.Difficulty * (blockNumber + 1));
            blockForRpc.Sha3Uncles = Keccak.OfAnEmptySequenceRlp;
            return blockForRpc;
        }

        // These functions assume 32 bytes of hash
        static Keccak GetValue(Cid deltaHash) => new Keccak(deltaHash.Hash.Digest);
        static Keccak GetValue(ByteString deltaHash) => new Keccak(deltaHash.ToByteArray());
    }
}
