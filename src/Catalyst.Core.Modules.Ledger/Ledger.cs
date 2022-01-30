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
using System.Linq;
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Dawn;
using Google.Protobuf;
using Lib.P2P;
using Nethermind.Core.Crypto;
using Nethermind.Db;
using Nethermind.State;
using Serilog;
using Serilog.Events;

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
        private readonly ITransactionRepository _receipts;
        private readonly IMempool<PublicEntryDao> _mempool;
        private readonly IMapperProvider _mapperProvider;
        private readonly IHashProvider _hashProvider;
        private readonly ILogger _logger;
        private readonly IDisposable _deltaUpdatesSubscription;

        private readonly object _synchronisationLock = new();
        volatile Cid _latestKnownDelta;
        long _latestKnownDeltaNumber = -1;

        public Cid LatestKnownDelta => _latestKnownDelta;

        public long LatestKnownDeltaNumber => Volatile.Read(ref _latestKnownDeltaNumber);

        private readonly IDeltaIndexService _deltaIndexService;

        private readonly ISynchroniser _synchroniser;

        public bool IsSynchonising => Monitor.IsEntered(_synchronisationLock);

        public Ledger(IDeltaExecutor deltaExecutor,
            IStateProvider stateProvider,
            IStorageProvider storageProvider,
            ISnapshotableDb stateDb,
            ISnapshotableDb codeDb,
            IAccountRepository accounts,
            IDeltaIndexService deltaIndexService,
            ITransactionRepository receipts,
            IDeltaHashProvider deltaHashProvider,
            ISynchroniser synchroniser,
            IMempool<PublicEntryDao> mempool,
            IMapperProvider mapperProvider,
            IHashProvider hashProvider,
            ILogger logger)
        {
            Accounts = accounts;
            _deltaExecutor = deltaExecutor;
            _stateProvider = stateProvider;
            _storageProvider = storageProvider;

            _stateDb = stateDb;
            _codeDb = codeDb;
            _mempool = mempool;
            _mapperProvider = mapperProvider;
            _hashProvider = hashProvider;
            _logger = logger;
            _receipts = receipts;
            _synchroniser = synchroniser;

            _deltaUpdatesSubscription = deltaHashProvider.DeltaHashUpdates.Subscribe(Update);

            _deltaIndexService = deltaIndexService;

            var latestDeltaIndex = _deltaIndexService.LatestDeltaIndex();
            if (latestDeltaIndex != null)
            {
                _latestKnownDelta = latestDeltaIndex.Cid;
                _latestKnownDeltaNumber = (long)latestDeltaIndex.Height;
                return;
            }

            _latestKnownDelta = _synchroniser.DeltaCache.GenesisHash;
            WriteLatestKnownDelta(_latestKnownDelta);
        }

        private void FlushTransactionsFromDelta(Cid deltaHash)
        {
            _synchroniser.DeltaCache.TryGetOrAddConfirmedDelta(deltaHash, out var delta);
            if (delta != null)
            {
                var deltaTransactions =
                    delta.PublicEntries.Select(x => x.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider));

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
                       .CacheDeltasBetween(LatestKnownDelta, deltaHash, CancellationToken.None)?
                       .Reverse()
                       .ToList();

                    if (!Equals(chainedDeltaHashes.First(), LatestKnownDelta))
                    {
                        _logger.Warning(
                            "Failed to walk back the delta chain to {LatestKnownDelta}, giving up ledger update.",
                            LatestKnownDelta);
                        return;
                    }

                    foreach (var chainedDeltaHash in chainedDeltaHashes.Skip(1))
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
                Environment.Exit(2);
            }
        }

        private void UpdateLedgerFromDelta(Cid deltaHash)
        {
            var stateSnapshot = _stateDb.TakeSnapshot();
            if (stateSnapshot != -1)
            {
                if (_logger.IsEnabled(LogEventLevel.Error))
                {
                    _logger.Error("Uncommitted state ({stateSnapshot}) when processing from a branch root {branchStateRoot} starting with delta {deltaHash}",
                        stateSnapshot,
                        null,
                        deltaHash);
                }
            }

            var snapshotStateRoot = _stateProvider.StateRoot;

            try
            {
                if (!_synchroniser.DeltaCache.TryGetOrAddConfirmedDelta(deltaHash, out var nextDeltaInChain))
                {
                    _logger.Warning("Failed to retrieve Delta with hash {hash} from the Dfs, ledger has not been updated.", deltaHash);
                    return;
                }

                Cid parentCid = Cid.Read(nextDeltaInChain.PreviousDeltaDfsHash.ToByteArray());
                if (!_synchroniser.DeltaCache.TryGetOrAddConfirmedDelta(parentCid, out Delta parentDelta))
                {
                    _logger.Warning("Failed to retrieve parent Delta with hash {hash} from the Dfs, ledger has not been updated.", deltaHash);
                    return;
                }

                ReceiptDeltaTracer tracer = new(nextDeltaInChain, deltaHash);

                // add here a receipts tracer or similar, depending on what data needs to be stored for each contract

                _stateProvider.Reset();
                _storageProvider.Reset();

                _stateProvider.StateRoot = new Keccak(parentDelta.StateRoot?.ToByteArray());
                _deltaExecutor.Execute(nextDeltaInChain, tracer);

                // store receipts
                if (tracer.Receipts.Any())
                {
                    _receipts.Put(deltaHash, tracer.Receipts.ToArray(), nextDeltaInChain.PublicEntries.ToArray());
                }

                _stateDb.Commit();
                _codeDb.Commit();

                _latestKnownDelta = deltaHash;

                WriteLatestKnownDelta(deltaHash);
            }
            catch
            {
                Restore(stateSnapshot, snapshotStateRoot);
            }
        }

        void WriteLatestKnownDelta(Cid deltaHash)
        {
            _latestKnownDelta = deltaHash;

            Volatile.Write(ref _latestKnownDeltaNumber, _latestKnownDeltaNumber + 1);
            _deltaIndexService.Map(_latestKnownDeltaNumber, deltaHash); // store delta numbers
            _synchroniser.UpdateState((ulong)_latestKnownDeltaNumber);
        }

        private void Restore(int stateSnapshot, Keccak snapshotStateRoot)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Reverting deltas {stateRoot}", _stateProvider.StateRoot);
            }

            _stateDb.Restore(stateSnapshot);
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
