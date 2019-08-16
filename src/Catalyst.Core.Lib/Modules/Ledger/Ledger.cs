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

using Catalyst.Common.Interfaces.Modules.Ledger;
using Dawn;
using Serilog;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Modules.Ledger.Models;
using Multiformats.Hash;

namespace Catalyst.Core.Lib.Modules.Ledger
{
    /// <summary>
    ///  This class represents a ledger and is a collection of accounts and data store.
    /// </summary>
    /// <seealso cref="Catalyst.Common.Interfaces.Modules.Ledger.ILedger" />
    public class Ledger : ILedger, IDisposable
    {
        public IAccountRepository Accounts { get; }
        private readonly IMempool _mempool;
        private readonly ILogger _logger;
        private readonly IDisposable _deltaUpdatesSubscription;

        public byte[] LedgerStateUpdate { get; set; }
 
        public Ledger(IAccountRepository accounts, IDeltaHashProvider deltaHashProvider, IMempool mempool, ILogger logger)
        {
            Accounts = accounts;
            _mempool = mempool;
            _logger = logger;

            _deltaUpdatesSubscription = deltaHashProvider.DeltaHashUpdates.Subscribe(FlushTransactionsFromDelta);
        }

        public void FlushTransactionsFromDelta(Multihash confirmedDelta)
        {
            var transactionsToFlush = _mempool.GetMemPoolContent().Select(d => d.DocumentId);
            _mempool.Delete(transactionsToFlush.ToArray());
        }

        public bool SaveAccountState(IAccount account)
        {
            Guard.Argument(account, nameof(account)).NotNull();

            try
            {
                Accounts.Add((Account) account);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to add account state to the Ledger");
                return false;
            }
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
        }
    }
}
