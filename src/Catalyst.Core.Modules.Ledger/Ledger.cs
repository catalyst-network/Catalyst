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
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Dawn;
using Google.Protobuf;
using LibP2P;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm;
using Nethermind.Evm.Tracing;
using Nethermind.Store;
using Serilog;
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
        private readonly IKvm _virtualMachine;
        private readonly IContractEntryExecutor _contractEntryExecutor;
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

        public Ledger(IKvm virtualMachine,
            IContractEntryExecutor contractEntryExecutor,
            IStateProvider stateProvider,
            ISpecProvider specProvider,
            IAccountRepository accounts,
            IDeltaHashProvider deltaHashProvider,
            ILedgerSynchroniser synchroniser,
            IMempool<TransactionBroadcastDao> mempool,
            ILogger logger)
        {
            Accounts = accounts;
            _virtualMachine = virtualMachine;
            _contractEntryExecutor = contractEntryExecutor;
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

                StateUpdate stateUpdate = ToStateUpdate(nextDeltaInChain);

                foreach (var entry in nextDeltaInChain.PublicEntries)
                {
                    UpdateLedgerAccountFromEntry(stateUpdate, entry);
                }

                foreach (var entry in nextDeltaInChain.ContractEntries)
                {
                    UpdateLedgerAccountFromEntry(stateUpdate, entry);
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

        private StateUpdate ToStateUpdate(Delta delta)
        {
            var gasLimit = 1_000_000L;
            StateUpdate result = new StateUpdate
            {
                Difficulty = 1,
                Number = 1,
                Timestamp = (UInt256)delta.TimeStamp.Seconds,
                GasLimit = gasLimit,
                /* here we can read coinbase entries from the delta
                   but we need to decide how to split fees and which one to pick for the KVM */
                GasBeneficiary = Address.Zero,
                GasUsed = 0L
            };

            return result;
        }

        private void UpdateLedgerAccountFromEntry(StateUpdate stateUpdate, PublicEntry entry)
        {
            ContractEntry contractEntry = new ContractEntry();
            contractEntry.Base = entry.Base;
            contractEntry.Amount = entry.Amount;
            contractEntry.Data = ByteString.Empty;
            UpdateLedgerAccountFromEntry(stateUpdate, contractEntry);
        }
        
        private void UpdateLedgerAccountFromEntry(StateUpdate stateUpdate, ContractEntry entry)
        {
            _contractEntryExecutor.Execute(entry, stateUpdate, NullTxTracer.Instance);
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
