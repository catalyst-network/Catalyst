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
using Catalyst.Protocol.Deltas;

namespace Catalyst.Abstractions.Consensus.Deltas
{
    /// <summary>
    /// This component is meant to be used to produce and retrieve ranking/voting data
    /// about the different candidate deltas observed on the network, in order to be
    /// able to determine which candidate should eventually make it to the DFS.
    /// </summary>
    /// <remarks>A producer will call that method at the start of the Campaigning phase.</remarks>
    public interface IDeltaVoter : IObserver<CandidateDeltaBroadcast>
    {
        bool TryGetFavouriteDelta(byte[] previousDeltaDfsHash, out FavouriteDeltaBroadcast favourite);
    }
}
