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
using Catalyst.Abstractions.Mempool.Models;
using Catalyst.Abstractions.Mempool.Repositories;
using Dawn;
using Serilog;

namespace Catalyst.Core.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public sealed class Mempool<T> : IMempool<T> where T : class, IMempoolItem
    {
        private readonly ILogger _logger;
        public IMempoolRepository<T> Repository { get; }

        /// <inheritdoc />
        public Mempool(IMempoolRepository<T> repository, ILogger logger)
        {
            Guard.Argument(repository, nameof(repository)).NotNull();
            Repository = repository;
            _logger = logger;
        }
    }
}
