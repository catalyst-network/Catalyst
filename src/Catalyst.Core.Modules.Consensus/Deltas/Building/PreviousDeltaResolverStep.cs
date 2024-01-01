#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.IO;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Protocol.Deltas;
using Serilog;
using Serilog.Events;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class PreviousDeltaResolverStep : IDeltaBuilderStep
    {
        private readonly IDeltaCache _deltaCache;
        private readonly ILogger _logger;

        public PreviousDeltaResolverStep(IDeltaCache deltaCache, ILogger logger)
        {
            _deltaCache = deltaCache ?? throw new ArgumentNullException(nameof(deltaCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Execute(DeltaBuilderContext context)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug)) _logger.Debug("Resolving previous delta by hash ({previousHash})", context.PreviousDeltaHash);
            if (!_deltaCache.TryGetOrAddConfirmedDelta(context.PreviousDeltaHash, out Delta previousDelta))
            {
                throw new InvalidDataException("Cannot retrieve previous delta from cache during delta construction.");
            }
            
            context.PreviousDelta = previousDelta;
        }
    }
}
