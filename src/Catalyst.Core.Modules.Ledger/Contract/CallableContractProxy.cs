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
using Catalyst.Abstractions.Contract;
using Catalyst.Abstractions.Kvm;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Lib.P2P;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Db;
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
        private readonly ISnapshotableDb _stateDb;
        private readonly ISnapshotableDb _codeDb;

        private static ulong DefaultContractGasLimit = 1_600_000L;

        public CallableContractProxy(IDeltaHashProvider deltaHashProvider, IDeltaCache deltaCache, IDeltaExecutor deltaExecutor, IStateProvider stateProvider, IStorageProvider storageProvider, ISnapshotableDb stateDb, ISnapshotableDb codeDb)
        {
            _deltaHashProvider = deltaHashProvider;
            _deltaCache = deltaCache;
            _deltaExecutor = deltaExecutor;
            _stateProvider = stateProvider;
            _storageProvider = storageProvider;
            _stateDb = stateDb;
            _codeDb = codeDb;
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

        private PublicEntry GenerateTransaction(Address contractAddress, Address sender, byte[] transactionData)
        {
            return new PublicEntry
            {
                //_stateProvider.GetNonce(sender)
                Nonce = (ulong)0,
                SenderAddress = sender.Bytes.ToByteString(),
                ReceiverAddress = contractAddress?.Bytes.ToByteString(),
                GasLimit = DefaultContractGasLimit,
                GasPrice = UInt256.Zero.ToUint256ByteString(),
                Amount = UInt256.Zero.ToUint256ByteString(),
                Data = transactionData?.ToByteString() ?? ByteString.Empty
            };
        }

        public byte[] Call(Address contractAddress, byte[] data, bool callAndRestore = true)
        {
            return Call(contractAddress, Address.SystemUser, data, callAndRestore);
        }

        public byte[] Call(Address contractAddress, Address sender, byte[] data, bool callAndRestore = true)
        {
            var transaction = GenerateTransaction(contractAddress, sender, data);

            var latestDeltaCid = _deltaHashProvider.GetLatestDeltaHash();
            _deltaCache.TryGetOrAddConfirmedDelta(latestDeltaCid, out Delta latestDelta);

            Keccak root = latestDelta.StateRoot.ToKeccak();

            var newDelta = CreateOneOffDelta(latestDeltaCid, latestDelta, transaction);

            CallOutputTracer callOutputTracer = new CallOutputTracer();

            _stateProvider.StateRoot = root;

            if (callAndRestore)
            {
                _deltaExecutor.CallAndReset(newDelta, callOutputTracer);
                _stateProvider.Reset();
                _storageProvider.Reset();
            }
            else
            {
                _deltaExecutor.Execute(newDelta, callOutputTracer);
                _stateDb.Commit();
                _codeDb.Commit();
            }

            return callOutputTracer.ReturnValue;
        }

        /// <summary>
        /// Creates <see cref="Address.SystemUser"/> account if its not in current state.
        /// </summary>
        public void EnsureSystemAccount()
        {
            if (!_stateProvider.AccountExists(Address.SystemUser))
            {
                _stateProvider.CreateAccount(Address.SystemUser, UInt256.Zero);
                _stateProvider.Commit(CatalystGenesisSpec.Instance);
            }
        }
    }
}
