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
    public interface IDeltaElector : IObserver<FavouriteDeltaBroadcast>
    {
        /// <summary>
        /// When the election phase is over, this method can be called to retrieve which candidate
        /// has been the most popular for a given cycle. If the candidate is popular enough, it
        /// will then be appointed as the next official delta.
        /// </summary>
        /// <remarks>This function will be called at the beginning of a Voting cycle.</remarks>
        /// <param name="previousDeltaDfsHash">The DFS hash of the delta for which we are
        /// trying to produce a successor.</param>
        /// <returns>The most popular candidate for a given cycle.</returns>
        CandidateDeltaBroadcast GetMostPopularCandidateDelta(byte[] previousDeltaDfsHash);
    }
}
