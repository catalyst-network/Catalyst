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

using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Core.Lib.Modules.Consensus.Cycle;
using Serilog;

namespace Catalyst.Core.Lib.Modules.Consensus
{
    public sealed class Consensus : IConsensus
    {
        public ICycleEventsProvider CycleEventsProvider { get; }
        public IDeltaBuilder DeltaBuilder { get; }
        public IDeltaHub DeltaHub { get; }
        public IDeltaHashProvider DeltaHashProvider { get; }

        public Consensus(IDeltaBuilder deltaBuilder,
            IDeltaHub deltaHub,
            ILogger logger,
            IDeltaHashProvider deltaHashProvider,
            ICycleEventsProvider cycleEventsProvider)
        {
            CycleEventsProvider = cycleEventsProvider;
            DeltaBuilder = deltaBuilder;
            DeltaHub = deltaHub;
            DeltaHashProvider = deltaHashProvider;
            logger.Information("Consensus service initialised.");
        }
    }
}
