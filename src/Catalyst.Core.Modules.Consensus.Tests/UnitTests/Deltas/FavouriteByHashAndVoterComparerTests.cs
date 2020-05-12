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

using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NUnit.Framework;
using System.Collections.Generic;
using CandidateDeltaBroadcast = Catalyst.Protocol.Wire.CandidateDeltaBroadcast;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public class FavouriteByHashAndVoterComparerTests
    {
        private static PeerId voter1 = PeerIdHelper.GetPeerId("voter1");
        private static PeerId voter2 = PeerIdHelper.GetPeerId("voter2");

        private static PeerId producer1 = PeerIdHelper.GetPeerId("producer1");
        private static PeerId producer2 = PeerIdHelper.GetPeerId("producer2");

        private static ByteString hash1 = ByteUtil.GenerateRandomByteArray(32).ToByteString();
        private static ByteString hash2 = ByteUtil.GenerateRandomByteArray(32).ToByteString();

        private static ByteString previousHash1 = ByteUtil.GenerateRandomByteArray(32).ToByteString();
        private static ByteString previousHash2 = ByteUtil.GenerateRandomByteArray(32).ToByteString();

        public static object[] FavouritesComparisonData = new object[]{
            new object[] { null, null, true },
            new object[] { new FavouriteDeltaBroadcast(), new FavouriteDeltaBroadcast(), true },
            //new object[] { null, new FavouriteDeltaBroadcast(), false },
            //new object[] { new FavouriteDeltaBroadcast(), null, false },
            //new object[] { new FavouriteDeltaBroadcast
            //    {
            //        Candidate = new CandidateDeltaBroadcast
            //        {
            //            Hash = hash1, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
            //        },
            //        VoterId = voter1
            //    },
            //    new FavouriteDeltaBroadcast
            //    {
            //        Candidate = new CandidateDeltaBroadcast
            //        {
            //            Hash = hash2, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
            //        },
            //        VoterId = voter1
            //    }, false },

            new object[] { new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
                    },
                    VoterId = voter1
                },
                new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
                    },
                    VoterId = voter2
                }, false },

            new object[] { new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, ProducerId = producer1, PreviousDeltaDfsHash = previousHash1
                    },
                    VoterId = voter1
                },
                new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, ProducerId = producer2, PreviousDeltaDfsHash = previousHash2
                    },
                    VoterId = voter1
                }, true}
        };

        //public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only()
        //{
        //    var comparisonResult = true;
        //    var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(null);
        //    var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(null);
        //    if (xHashCode != 0 && yHashCode != 0)
        //    {
        //        xHashCode.Equals(yHashCode).Should().Be(comparisonResult);
        //    }

        //    FavouriteByHashAndVoterComparer.Default.Equals(x, y).Should().Be(comparisonResult);
        //}

        [TestCaseSource(nameof(FavouritesComparisonData))]
        public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y, bool comparisonResult)
        {
            var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(x);
            var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(y);
            if (xHashCode != 0 && yHashCode != 0)
            {
                xHashCode.Equals(yHashCode).Should().Be(comparisonResult);
            }

            FavouriteByHashAndVoterComparer.Default.Equals(x, y).Should().Be(comparisonResult);
        }

        //[TestCaseSource(nameof(FavouritesComparisonData))]
        //public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y, bool comparisonResult)
        //{
        //    var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(x);
        //    var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(y);
        //    if (xHashCode != 0 && yHashCode != 0)
        //    {
        //        xHashCode.Equals(yHashCode).Should().Be(comparisonResult);
        //    }

        //    FavouriteByHashAndVoterComparer.Default.Equals(x, y).Should().Be(comparisonResult);
        //}

        //[TestCaseSource(nameof(FavouritesComparisonData))]
        //public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y, bool comparisonResult)
        //{
        //    var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(x);
        //    var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(y);
        //    if (xHashCode != 0 && yHashCode != 0)
        //    {
        //        xHashCode.Equals(yHashCode).Should().Be(comparisonResult);
        //    }

        //    FavouriteByHashAndVoterComparer.Default.Equals(x, y).Should().Be(comparisonResult);
        //}

        //[TestCaseSource(nameof(FavouritesComparisonData))]
        //public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y, bool comparisonResult)
        //{
        //    var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(x);
        //    var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(y);
        //    if (xHashCode != 0 && yHashCode != 0)
        //    {
        //        xHashCode.Equals(yHashCode).Should().Be(comparisonResult);
        //    }

        //    FavouriteByHashAndVoterComparer.Default.Equals(x, y).Should().Be(comparisonResult);
        //}

        //[TestCaseSource(nameof(FavouritesComparisonData))]
        //public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y, bool comparisonResult)
        //{
        //    var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(x);
        //    var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(y);
        //    if (xHashCode != 0 && yHashCode != 0)
        //    {
        //        xHashCode.Equals(yHashCode).Should().Be(comparisonResult);
        //    }

        //    FavouriteByHashAndVoterComparer.Default.Equals(x, y).Should().Be(comparisonResult);
        //}

        //[TestCaseSource(nameof(FavouritesComparisonData))]
        //public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y, bool comparisonResult)
        //{
        //    var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(x);
        //    var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(y);
        //    if (xHashCode != 0 && yHashCode != 0)
        //    {
        //        xHashCode.Equals(yHashCode).Should().Be(comparisonResult);
        //    }

        //    FavouriteByHashAndVoterComparer.Default.Equals(x, y).Should().Be(comparisonResult);
        //}
    }
}
