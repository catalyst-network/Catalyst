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

using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Contract;
using Catalyst.Abstractions.Kvm;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Lib.P2P;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm.Tracing;
using Nethermind.State;

namespace Catalyst.Core.Modules.Ledger.Contract
{
    public class CallableContractProxy : ICallableContractProxy
    {
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IDeltaCache _deltaCache;
        private readonly IDeltaExecutor _deltaExecutor;
        private readonly IStateProvider _stateProvider;
        private readonly IStorageProvider _storageProvider;

        private static ulong DefaultContractGasLimit = 1_600_000L;

        public CallableContractProxy(IDeltaHashProvider deltaHashProvider, IDeltaCache deltaCache, IDeltaExecutor deltaExecutor, IStateProvider stateProvider, IStorageProvider storageProvider)
        {
            _deltaHashProvider = deltaHashProvider;
            _deltaCache = deltaCache;
            _deltaExecutor = deltaExecutor;
            _stateProvider = stateProvider;
            _storageProvider = storageProvider;
        }

        public Delta CreateOneOffDelta(Cid cid, Delta delta, PublicEntry publicEntry)
        {
            Delta newDelta = delta.Clone();
            newDelta.PreviousDeltaDfsHash = cid.ToArray().ToByteString();
            newDelta.CoinbaseEntries.Clear();
            newDelta.ConfidentialEntries.Clear();
            newDelta.PublicEntries.Clear();
            newDelta.PublicEntries.Add(publicEntry);
            return newDelta;
        }

        private PublicEntry GenerateTransaction(Address contractAddress, byte[] transactionData)
        {
            return new PublicEntry
            {
                Nonce = 0,
                SenderAddress = Address.SystemUser.Bytes.ToByteString(),
                ReceiverAddress = contractAddress?.Bytes.ToByteString(),
                GasLimit = DefaultContractGasLimit,
                GasPrice = UInt256.Zero.ToUint256ByteString(),
                Amount = UInt256.Zero.ToUint256ByteString(),
                Data = transactionData?.ToByteString() ?? ByteString.Empty
            };
        }

        public byte[] Call(Address contractAddress, byte[] data)
        {
            var transaction = GenerateTransaction(contractAddress, data);

            var latestDeltaCid = _deltaHashProvider.GetLatestDeltaHash();
            _deltaCache.TryGetOrAddConfirmedDelta(latestDeltaCid, out Delta latestDelta);

            Keccak root = latestDelta.StateRoot.ToKeccak();

            var newDelta = CreateOneOffDelta(latestDeltaCid, latestDelta, transaction);

            CallOutputTracer callOutputTracer = new();

            _stateProvider.StateRoot = root;
            _deltaExecutor.CallAndReset(newDelta, callOutputTracer);
            _stateProvider.Reset();
            _storageProvider.Reset();

            return callOutputTracer.ReturnValue;
        }
    }
}
