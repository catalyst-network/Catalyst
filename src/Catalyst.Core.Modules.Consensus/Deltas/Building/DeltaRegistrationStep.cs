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
using Catalyst.Abstractions.Consensus.Deltas;
using Serilog;
using Serilog.Events;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class DeltaRegistrationStep : IDeltaBuilderStep
    {
        private readonly IDeltaCache _deltaCache;
        private readonly ILogger _logger;

        public DeltaRegistrationStep(IDeltaCache deltaCache, ILogger logger)
        {
            _deltaCache = deltaCache ?? throw new ArgumentNullException(nameof(deltaCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Execute(DeltaBuilderContext context)
        {
            if (_logger.IsEnabled(LogEventLevel.Debug)) _logger.Debug("Registering new delta with parent ({previousHash})", context.PreviousDeltaHash);
            _deltaCache.AddLocalDelta(context.Candidate, context.ProducedDelta);

            // for easier delta tracking when testing truffle
            // if ((context.ProducedDelta.PublicEntries?.Count ?? 0) > 0)
            // {
            //     _deltaCache.AddLocalDelta(context.Candidate, context.ProducedDelta);
            // }
        }
    }
}
