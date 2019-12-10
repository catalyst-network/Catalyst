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
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Dawn;
using LibP2P;
using Nethermind.Core.Crypto;
using Nethermind.Evm.Tracing;
using Nethermind.Store;
using Serilog;
using Serilog.Events;
using Account = Catalyst.Abstractions.Ledger.Models.Account;

namespace Catalyst.Core.Modules.Ledger
{
    /// <summary>
    ///     This class represents a ledger and is a collection of accounts and data store.
    /// </summary>
    /// <inheritdoc cref="ILedger" />
    /// <inheritdoc cref="IDisposable" />
    public sealed class Ledger : ILedger, IDisposable
    {
        public IAccountRepository Accounts { get; }
        private readonly IDeltaExecutor _deltaExecutor;
        private readonly IStateProvider _stateProvider;
        private readonly IStorageProvider _storageProvider;
        private readonly ISnapshotableDb _stateDb;
        private readonly ISnapshotableDb _codeDb;
        private readonly IDeltaByNumberRepository _deltas;
        private readonly ILedgerSynchroniser _synchroniser;
        private readonly IMempool<PublicEntryDao> _mempool;
        private readonly IMapperProvider _mapperProvider;
        private readonly ILogger _logger;
        private readonly IDisposable _deltaUpdatesSubscription;

        private readonly object _synchronisationLock = new object();

        public Cid LatestKnownDelta { get; private set; }
        public long LatestKnownDeltaNumber { get; private set; }
        public bool IsSynchonising => Monitor.IsEntered(_synchronisationLock);

        public Ledger(IDeltaExecutor deltaExecutor,
            IStateProvider stateProvider,
            IStorageProvider storageProvider,
            ISnapshotableDb stateDb,
            ISnapshotableDb codeDb,
            IAccountRepository accounts,
            IDeltaByNumberRepository deltas,
            IDeltaHashProvider deltaHashProvider,
            ILedgerSynchroniser synchroniser,
            IMempool<PublicEntryDao> mempool,
            IMapperProvider mapperProvider,
            ILogger logger)
        {
            Accounts = accounts;
            _deltaExecutor = deltaExecutor;
            _stateProvider = stateProvider;
            _storageProvider = storageProvider;
            _stateDb = stateDb;
            _codeDb = codeDb;
            _deltas = deltas;
            _synchroniser = synchroniser;
            _mempool = mempool;
            _mapperProvider = mapperProvider;
            _logger = logger;

            _deltaUpdatesSubscription = deltaHashProvider.DeltaHashUpdates.Subscribe(Update);
            LatestKnownDelta = _synchroniser.DeltaCache.GenesisHash;
        }

        private void FlushTransactionsFromDelta(Cid deltaHash)
        {
            _synchroniser.DeltaCache.TryGetOrAddConfirmedDelta(deltaHash, out var delta);
            if (delta != null)
            {
                var deltaTransactions = delta.PublicEntries.Select(x => x.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider));
                _mempool.Service.Delete(deltaTransactions);
            }
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

                FlushTransactionsFromDelta(deltaHash);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to update the ledger using the delta with hash {deltaHash}",
                    deltaHash);
            }
        }

        private void UpdateLedgerFromDelta(Cid deltaHash)
        {
            var stateSnapshot = _stateDb.TakeSnapshot();
            var codeSnapshot = _codeDb.TakeSnapshot();
            if (stateSnapshot != -1 || codeSnapshot != -1)
            {
                if (_logger.IsEnabled(LogEventLevel.Error))
                {
                    _logger.Error("Uncommitted state ({stateSnapshot}, {codeSnapshot}) when processing from a branch root {branchStateRoot} starting with delta {deltaHash}",
                        stateSnapshot,
                        codeSnapshot,
                        null,
                        deltaHash);
                }
            }

            var snapshotStateRoot = _stateProvider.StateRoot;

            // this code should be brought in / used as a reference if reorganization behaviour is known
            //// if (branchStateRoot != null && _stateProvider.StateRoot != branchStateRoot)
            //// {
            ////     /* discarding the other branch data - chain reorganization */
            ////     Metrics.Reorganizations++;
            ////     _storageProvider.Reset();
            ////     _stateProvider.Reset();
            ////     _stateProvider.StateRoot = branchStateRoot;
            //// }

            try
            {
                if (!_synchroniser.DeltaCache.TryGetOrAddConfirmedDelta(deltaHash, out Delta nextDeltaInChain))
                {
                    _logger.Warning(
                        "Failed to retrieve Delta with hash {hash} from the Dfs, ledger has not been updated.",
                        deltaHash);
                    return;
                }

                // add here a receipts tracer or similar, depending on what data needs to be stored for each contract
                _deltaExecutor.Execute(nextDeltaInChain, NullTxTracer.Instance);

                // this code should be brought in / used as a reference if reorganization behaviour is known
                //// delta testing here (for delta production)
                //// if ((options & ProcessingOptions.ReadOnlyChain) != 0)
                //// {
                ////     Restore(stateSnapshot, codeSnapshot, snapshotStateRoot);
                //// }
                //// else
                //// {
                ////   _stateDb.Commit();
                ////   _codeDb.Commit();
                //// }

                _stateDb.Commit();
                _codeDb.Commit();

                // store delta numbers
                _deltas.Map(LatestKnownDeltaNumber, deltaHash);

                LatestKnownDelta = deltaHash;
                LatestKnownDeltaNumber += 1;
            }
            catch
            {
                Restore(stateSnapshot, codeSnapshot, snapshotStateRoot);
            }
        }

        private void Restore(int stateSnapshot, int codeSnapshot, Keccak snapshotStateRoot)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Reverting deltas {stateRoot}", _stateProvider.StateRoot);
            }

            _stateDb.Restore(stateSnapshot);
            _codeDb.Restore(codeSnapshot);
            _storageProvider.Reset();
            _stateProvider.Reset();
            _stateProvider.StateRoot = snapshotStateRoot;
            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Reverted deltas {stateRoot}", _stateProvider.StateRoot);
            }
        }

        private void Dispose(bool disposing)
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
