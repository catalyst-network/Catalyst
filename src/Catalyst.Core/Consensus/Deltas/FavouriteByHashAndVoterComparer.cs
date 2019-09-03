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

using System.Collections.Generic;
using Catalyst.Core.Util;
using Catalyst.Protocol.Deltas;
using Google.Protobuf;

namespace Catalyst.Core.Consensus.Deltas
{
    public class FavouriteByHashAndVoterComparer : IEqualityComparer<FavouriteDeltaBroadcast>,
        IComparer<FavouriteDeltaBroadcast>
    {
        public int Compare(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(null, y))
            {
                return 1;
            }

            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            var candidateHashComparison = CompareCandidateHash(x.Candidate, y.Candidate);

            if (candidateHashComparison != 0)
            {
                return candidateHashComparison;
            }

            return ByteUtil.ByteListComparer.Default.Compare(
                x.VoterId?.ToByteArray(),
                y.VoterId?.ToByteArray());
        }

        private static int CompareCandidateHash(CandidateDeltaBroadcast x, CandidateDeltaBroadcast y)
        {
            var xByteArray = x?.Hash?.ToByteArray();
            var yByteArray = y?.Hash?.ToByteArray();
            return 
                ByteUtil.ByteListMinSizeComparer.Default.Compare(
                    xByteArray,
                    yByteArray);
        }

        public static IEqualityComparer<FavouriteDeltaBroadcast> Default { get; } = new FavouriteByHashAndVoterComparer();

        public bool Equals(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(FavouriteDeltaBroadcast favourite)
        {
            if (favourite == null)
            {
                return 0;
            }

            unchecked
            {
                var candidateHash = favourite.Candidate?.Hash == null ? 0 : favourite.Candidate.Hash.GetHashCode();
                var voterIdHash = favourite.VoterId == null ? 0 : favourite.VoterId.GetHashCode();
                return (candidateHash * 397) ^ voterIdHash;
            }
        }
    }
}
