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
using System.Linq;
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Transaction;
using Dawn;
using Google.Protobuf;
using LibP2P;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Evm.Tracing;
using Nethermind.Store;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiBase;
using Account = Catalyst.Core.Modules.Ledger.Models.Account;

namespace Catalyst.Core.Modules.Ledger
{
    /// <summary>
    ///     This class represents a ledger and is a collection of accounts and data store.
    /// </summary>
    /// <inheritdoc cref="ILedger" />
    /// <inheritdoc cref="IDisposable" />
    public class Ledger : ILedger, IDisposable
    {
        public IAccountRepository Accounts { get; }
        private readonly IKvm _kvm;
        private readonly IStateProvider _stateProvider;
        private readonly ISpecProvider _specProvider;
        private readonly ILedgerSynchroniser _synchroniser;
        private readonly IMempool<TransactionBroadcastDao> _mempool;
        private readonly ILogger _logger;
        private readonly IDisposable _deltaUpdatesSubscription;

        private readonly object _synchronisationLock = new object();
        private readonly ICryptoContext _cryptoContext;

        public Cid LatestKnownDelta { get; private set; }
        public bool IsSynchonising => Monitor.IsEntered(_synchronisationLock);

        public Ledger(IKvm kvm,
            IStateProvider stateProvider,
            ISpecProvider specProvider,
            IAccountRepository accounts,
            IDeltaHashProvider deltaHashProvider,
            ILedgerSynchroniser synchroniser,
            IMempool<TransactionBroadcastDao> mempool,
            ILogger logger)
        {
            Accounts = accounts;
            _kvm = kvm;
            _stateProvider = stateProvider;
            _specProvider = specProvider;
            _synchroniser = synchroniser;
            _mempool = mempool;
            _logger = logger;

            _deltaUpdatesSubscription = deltaHashProvider.DeltaHashUpdates.Subscribe(Update);
            LatestKnownDelta = _synchroniser.DeltaCache.GenesisHash;
            _cryptoContext = new FfiWrapper();
        }

        private void FlushTransactionsFromDelta()
        {
            var transactionsToFlush = _mempool.Repository.GetAll(); //TOD0 no get alls
            _mempool.Repository.Delete(transactionsToFlush);
        }

        /// <inheritdoc />
        public bool SaveAccountState(Account account)
        {
            Guard.Argument(account, nameof(account)).NotNull();

            try
            {
                Accounts.Add(account);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to add account state to the Ledger");
                return false;
            }
        }

        /// <inheritdoc />
        public void Update(Cid deltaHash)
        {
            try
            {
                lock (_synchronisationLock)
                {
                    var chainedDeltaHashes = _synchroniser
                       .CacheDeltasBetween(LatestKnownDelta, deltaHash, CancellationToken.None)
                       .Reverse()
                       .ToList();

                    if (!Equals(chainedDeltaHashes.First(), LatestKnownDelta))
                    {
                        _logger.Warning(
                            "Failed to walk back the delta chain to {LatestKnownDelta}, giving up ledger update.",
                            LatestKnownDelta);
                        return;
                    }

                    foreach (var chainedDeltaHash in chainedDeltaHashes)
                    {
                        UpdateLedgerFromDelta(chainedDeltaHash);
                    }
                }

                //https://github.com/catalyst-network/Catalyst.Node/issues/871
                FlushTransactionsFromDelta();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to update the ledger using the delta with hash {deltaHash}",
                    deltaHash);
            }
        }

        private void UpdateLedgerFromDelta(Cid deltaHash)
        {
            int snapshot = -1;
            try
            {
                snapshot = _stateProvider.TakeSnapshot();
                if (!_synchroniser.DeltaCache.TryGetOrAddConfirmedDelta(deltaHash,
                    out var nextDeltaInChain))
                {
                    _logger.Warning(
                        "Failed to retrieve Delta with hash {hash} from the Dfs, ledger has not been updated.",
                        deltaHash);
                    return;
                }

                foreach (var entry in nextDeltaInChain.PublicEntries)
                {
                    UpdateLedgerAccountFromEntry(entry);
                }

                foreach (var entry in nextDeltaInChain.ContractEntries)
                {
                    UpdateLedgerAccountFromEntry(entry);
                }

                LatestKnownDelta = deltaHash;

                _stateProvider.Commit(_specProvider.GenesisSpec);
            }
            catch
            {
                if (snapshot != -1)
                {
                    _stateProvider.Restore(snapshot);
                }
            }
        }

        private void UpdateLedgerAccountFromEntry(ContractEntry entry)
        {
            var executionType = entry.IsValidDeploymentEntry ? ExecutionType.Create : ExecutionType.Call;
            UpdateLedgerAccountFromEntry(entry.Base, entry.Amount, executionType, entry.Data.ToByteArray());
        }

        private void UpdateLedgerAccountFromEntry(PublicEntry entry) { UpdateLedgerAccountFromEntry(entry.Base, entry.Amount, ExecutionType.Transaction, Array.Empty<byte>()); }

        // <<<<<<< HEAD
        //             //todo: get an address from the key using the Account class from Common lib
        //             var account = Accounts.Get(pubKey.Bytes.ToBase32());
        // =======
        private void UpdateLedgerAccountFromEntry(BaseEntry entry, ByteString entryAmount, ExecutionType executionType, byte[] data)
        {
            Address GetAccountAddress(ByteString publicKey)
            {
                var pubKey = _cryptoContext.GetPublicKeyFromBytes(publicKey.ToByteArray());

                // todo: might need to trim or hash
                return pubKey.ToKvmAddress();
            }

            var receiver = GetAccountAddress(entry.ReceiverPublicKey);
            var sender = GetAccountAddress(entry.SenderPublicKey);
            var value = entryAmount.ToUInt256();
            var gasLimit = 1_000_000L;

            // these values will be set by the tx processor within the state update logic
            ExecutionEnvironment env = new ExecutionEnvironment
            {
                Originator = sender,
                Sender = sender,
                CodeSource = receiver,
                ExecutingAccount = receiver,
                Value = value,
                TransferValue = value,
                GasPrice = 0,
                InputData = data,
                CallDepth = 0,
                CurrentBlock = new StateUpdate
                {
                    Difficulty = 1,
                    Number = 1,
                    Timestamp = 1,
                    GasLimit = gasLimit,
                    GasBeneficiary = Address.Zero,
                    GasUsed = 0L
                },
                CodeInfo = _kvm.GetCachedCodeInfo(receiver)
            };

            VmState state = new VmState(gasLimit, env, executionType, false, true, false);
            _kvm.Run(state, NullTxTracer.Instance);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Accounts?.Dispose();
            _deltaUpdatesSubscription?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
