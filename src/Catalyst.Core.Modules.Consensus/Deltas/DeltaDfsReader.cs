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
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Dfs;
using Catalyst.Protocol.Deltas;
using LibP2P;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc />
    public class DeltaDfsReader : IDeltaDfsReader
    {
        private readonly IDfs _dfs;
        private readonly ILogger _logger;

        public DeltaDfsReader(IDfs dfs, ILogger logger)
        {
            _dfs = dfs;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool TryReadDeltaFromDfs(Cid cid,
            out Delta delta,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var responseStream = _dfs.ReadAsync(cid, cancellationToken)
                   .ConfigureAwait(false)
                   .GetAwaiter()
                   .GetResult()
                )
                {
                    var uncheckedDelta = Delta.Parser.ParseFrom(responseStream);
                    var isValid = uncheckedDelta.IsValid();
                    if (!isValid)
                    {
                        _logger.Warning("Retrieved an invalid delta from the Dfs at address {hash}");
                        delta = default;
                        return false;
                    }

                    delta = uncheckedDelta;
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to retrieve delta with hash {0} from the Dfs", cid);
                delta = default;
                return false;
            }
        }
    }
}
