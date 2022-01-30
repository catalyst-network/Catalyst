#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Collections.Generic;
using Catalyst.Abstractions.Kvm.Models;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Repository;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Web3.Controllers.Handlers;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Lib.P2P;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm.Tracing;

namespace Catalyst.Core.Modules.Web3
{
    public static class Web3EthApiExtensions
    {
        public static DeltaWithCid GetLatestDeltaWithCid(this IWeb3EthApi api)
        {
            return GetDeltaWithCid(api, api.DeltaResolver.LatestDelta);
        }

        public static DeltaWithCid GetDeltaWithCid(this IWeb3EthApi api, Cid cid)
        {
            if (!api.DeltaCache.TryGetOrAddConfirmedDelta(cid, out Delta delta))
            {
                throw new Exception($"Delta not found '{cid}'");
            }

            return new DeltaWithCid
            {
                Delta = delta,
                Cid = cid
            };
        }

        public static bool TryGetDeltaWithCid(this IWeb3EthApi api, BlockParameter block, out DeltaWithCid delta)
        {
            Cid cid;
            switch (block.Type)
            {
                case BlockParameterType.Earliest:
                    cid = api.DeltaCache.GenesisHash;
                    break;
                case BlockParameterType.Latest:
                    cid = api.DeltaResolver.LatestDelta;
                    break;
                case BlockParameterType.Pending:
                    cid = api.DeltaResolver.LatestDelta;
                    break;
                case BlockParameterType.BlockNumber:
                    var blockNumber = block.BlockNumber.Value;
                    if (!api.DeltaResolver.TryResolve(blockNumber, out cid))
                    {
                        delta = default;
                        return false;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            delta = api.GetDeltaWithCid(cid);
            return true;
        }

        public static PublicEntry ToPublicEntry(this IWeb3EthApi api, TransactionForRpc transactionCall, Keccak root)
        {
            return new PublicEntry
            {
                Nonce = (ulong)api.StateReader.GetNonce(root, transactionCall.From),
                SenderAddress = transactionCall.From.Bytes.ToByteString(),
                ReceiverAddress = transactionCall.To?.Bytes.ToByteString() ?? ByteString.Empty,
                GasLimit = (ulong)transactionCall.Gas.GetValueOrDefault(),
                GasPrice = transactionCall.GasPrice.GetValueOrDefault().ToUint256ByteString(),
                Amount = transactionCall.Value.GetValueOrDefault().ToUint256ByteString(),
                Data = transactionCall.Data?.ToByteString() ?? ByteString.Empty
            };
        }

        public static TransactionForRpc ToTransactionForRpc(this IWeb3EthApi api, DeltaWithCid deltaWithCid, int transactionIndex)
        {
            var (delta, deltaCid) = deltaWithCid;
            var publicEntry = delta.PublicEntries[transactionIndex];
            var deltaNumber = delta.DeltaNumber;

            return new TransactionForRpc
            {
                GasPrice = publicEntry.GasPrice.ToUInt256(),
                BlockHash = deltaCid,
                BlockNumber = (UInt256)deltaNumber,
                Nonce = publicEntry.Nonce,
                To = ToAddress(publicEntry.ReceiverAddress),
                From = ToAddress(publicEntry.SenderAddress),
                Value = publicEntry.Amount.ToUInt256(),
                Hash = publicEntry.GetHash(api.HashProvider),
                Data = publicEntry.Data.ToByteArray(),
                R = new byte[0],
                S = new byte[0],
                V = UInt256.Zero,
                Gas = publicEntry.GasLimit,
                TransactionIndex = (UInt256)transactionIndex
            };
        }

        public static IEnumerable<TransactionForRpc> ToTransactionsForRpc(this IWeb3EthApi api, DeltaWithCid deltaWithCid)
        {
            var (delta, deltaCid) = deltaWithCid;
            var publicEntries = delta.PublicEntries;
            var deltaNumber = delta.DeltaNumber;

            for (var i = 0; i < publicEntries.Count; i++)
            {
                var publicEntry = publicEntries[i];
                yield return new TransactionForRpc
                {
                    GasPrice = publicEntry.GasPrice.ToUInt256(),
                    BlockHash = deltaCid,
                    BlockNumber = (UInt256)deltaNumber,
                    Nonce = publicEntry.Nonce,
                    To = ToAddress(publicEntry.ReceiverAddress),
                    From = ToAddress(publicEntry.SenderAddress),
                    Value = publicEntry.Amount.ToUInt256(),
                    Hash = publicEntry.GetHash(api.HashProvider),
                    Data = publicEntry.Data.ToByteArray(),
                    R = new byte[0],
                    S = new byte[0],
                    V = UInt256.Zero,
                    Gas = publicEntry.GasLimit,
                    TransactionIndex = (UInt256)i
                };
            }
        }

        public static Address ToAddress(ByteString address)
        {
            if (address == null || address.IsEmpty)
            {
                return null;
            }

            return new Address(address.ToByteArray());
        }

        public static CallOutputTracer CallAndRestore(this IWeb3EthApi api, TransactionForRpc transactionCall, DeltaWithCid deltaWithCid)
        {
            var parentDelta = deltaWithCid.Delta;
            Keccak root = parentDelta.StateRoot.ToKeccak();

            if (transactionCall.Gas == null)
            {
                transactionCall.Gas = parentDelta.GasLimit;
            }

            var publicEntry = api.ToPublicEntry(transactionCall, root);

            var newDelta = deltaWithCid.CreateOneOffDelta(publicEntry);

            CallOutputTracer callOutputTracer = new();

            api.StateProvider.StateRoot = root;
            api.Executor.CallAndReset(newDelta, callOutputTracer);
            api.StateProvider.Reset();
            api.StorageProvider.Reset();
            return callOutputTracer;
        }
    }

    public struct DeltaWithCid
    {
        public Delta Delta;
        public Cid Cid;

        /// <summary>
        /// Creates a delta for one off execution.
        /// </summary>
        public Delta CreateOneOffDelta(PublicEntry publicEntry)
        {
            Delta newDelta = Delta.Clone();

            newDelta.PreviousDeltaDfsHash = Cid.ToArray().ToByteString();
            newDelta.CoinbaseEntries.Clear();
            newDelta.ConfidentialEntries.Clear();
            newDelta.PublicEntries.Clear();
            newDelta.PublicEntries.Add(publicEntry);
            return newDelta;
        }

        public void Deconstruct(out Delta delta, out Cid cid)
        {
            delta = Delta;
            cid = Cid;
        }
    }
}
