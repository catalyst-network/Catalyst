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

using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;

namespace Catalyst.Common.Interfaces.Modules.Consensus
{
    public interface IConsensus
    {
        /// <see cref="IDeltaBuilder" />
        IDeltaBuilder DeltaBuilder { get; }

        /// <see cref="IDeltaHub" />
        IDeltaHub DeltaHub { get; }

        /// <see cref="IDeltaHashProvider"/>
        IDeltaHashProvider DeltaHashProvider { get; }

        /// <see cref="ICycleEventsProvider"/>
        ICycleEventsProvider CycleEventsProvider { get; }

        /// <summary>
        /// Call this method to try and start acting as a delta producer.
        /// In effect this will try to start a <see cref="ICycleEventsProvider"/>
        /// if the node has been configured to have that feature.
        /// </summary>
        /// <returns><see cref="true"/> if the node can be a producer and
        /// has managed to start its <see cref="ICycleEventsProvider"/>,
        /// <see cref="false"/> otherwise.</returns>
        // bool TryStartActingAsProducer();

        /// <summary>
        /// This method will cancel the delta production cycle started on the
        /// node.
        /// </summary>
        /// <returns><see cref="true"/> if the production cycle stopped
        /// <see cref="false"/> otherwise.</returns>
        // bool StopActingAsProducer();
    }
}
