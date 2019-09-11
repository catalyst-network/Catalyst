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

using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Core.Lib.Mempool.Documents;
using Dawn;
using Serilog;

namespace Catalyst.Core.Modules.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public sealed class Mempool : IMempool<MempoolDocument>
    {
        private readonly ILogger _logger;
        public IMempoolRepository<MempoolDocument> Repository { get; }

        /// <inheritdoc />
        public Mempool(IMempoolRepository<MempoolDocument> transactionStore, ILogger logger)
        {
            Guard.Argument(transactionStore, nameof(transactionStore)).NotNull();
            Repository = transactionStore;
            _logger = logger;
        }
    }
}
