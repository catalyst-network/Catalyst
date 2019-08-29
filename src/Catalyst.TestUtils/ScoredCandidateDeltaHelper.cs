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

using Catalyst.Core.Consensus.Deltas;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Multiformats.Hash.Algorithms;

namespace Catalyst.TestUtils
{
    public static class ScoredCandidateDeltaHelper
    {
        public static ScoredCandidateDelta GetScoredCandidateDelta(CandidateDeltaBroadcast candidate = default,
            IMultihashAlgorithm hashAlgorithm = null,
            int score = 0)
        {
            var candidateDelta = candidate ?? DeltaHelper.GetCandidateDelta(hashAlgorithm: hashAlgorithm);
            return new ScoredCandidateDelta(candidateDelta, score);
        }

        public static ScoredCandidateDelta GetScoredCandidateDelta(byte[] previousDeltaHash = null,
            IMultihashAlgorithm hashAlgorithm = null,
            byte[] hash = null,
            PeerId producerId = null,
            int score = 0)
        {
            var candidateDelta = DeltaHelper.GetCandidateDelta(previousDeltaHash, hash, producerId, hashAlgorithm);
            return new ScoredCandidateDelta(candidateDelta, score);
        }
    }
}
