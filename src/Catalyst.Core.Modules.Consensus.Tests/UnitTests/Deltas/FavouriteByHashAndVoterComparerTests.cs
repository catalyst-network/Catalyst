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
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using MultiFormats;
using NUnit.Framework;
using CandidateDeltaBroadcast = Catalyst.Protocol.Wire.CandidateDeltaBroadcast;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public class FavouriteByHashAndVoterComparerTests
    {
        private static MultiAddress voter1 = MultiAddressHelper.GetAddress("voter1");
        private static MultiAddress voter2 = MultiAddressHelper.GetAddress("voter2");

        private static MultiAddress producer1 = MultiAddressHelper.GetAddress("producer1");
        private static MultiAddress producer2 = MultiAddressHelper.GetAddress("producer2");

        private static ByteString hash1 = ByteUtil.GenerateRandomByteArray(32).ToByteString();
        private static ByteString hash2 = ByteUtil.GenerateRandomByteArray(32).ToByteString();

        private static ByteString previousHash1 = ByteUtil.GenerateRandomByteArray(32).ToByteString();
        private static ByteString previousHash2 = ByteUtil.GenerateRandomByteArray(32).ToByteString();

        public class FavouritesTestData
        {
            public FavouriteDeltaBroadcast X { private set; get; }
            public FavouriteDeltaBroadcast Y { private set; get; }
            public bool ComparisonResult { private set; get; }
            public FavouritesTestData(FavouriteDeltaBroadcast x, FavouriteDeltaBroadcast y, bool comparisonResult)
            {
                X = x;
                Y = y;
                ComparisonResult = comparisonResult;
            }
        }

        private static FavouritesTestData[] FavouritesComparisonData = new FavouritesTestData[]{
            new FavouritesTestData(null, null, true),
            new FavouritesTestData(new FavouriteDeltaBroadcast(), new FavouriteDeltaBroadcast(), true ),
            new FavouritesTestData(null, new FavouriteDeltaBroadcast(), false ),
            new FavouritesTestData(new FavouriteDeltaBroadcast(), null, false ),
            new FavouritesTestData(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, Producer = producer1.GetKvmAddressByteString(), PreviousDeltaDfsHash = previousHash1
                    },
                    Voter = voter1.GetKvmAddressByteString()
                },
                new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash2, Producer = producer1.GetKvmAddressByteString(), PreviousDeltaDfsHash = previousHash1
                    },
                    Voter = voter1.GetKvmAddressByteString()
                }, false ),

           new FavouritesTestData(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, Producer = producer1.GetKvmAddressByteString(), PreviousDeltaDfsHash = previousHash1
                    },
                    Voter = voter1.GetKvmAddressByteString()
                },
                new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, Producer = producer1.GetKvmAddressByteString(), PreviousDeltaDfsHash = previousHash1
                    },
                    Voter = voter2.GetKvmAddressByteString()
                }, false ),

            new FavouritesTestData(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, Producer = producer1.GetKvmAddressByteString(), PreviousDeltaDfsHash = previousHash1
                    },
                    Voter = voter1.GetKvmAddressByteString()
                },
                new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        Hash = hash1, Producer = producer2.GetKvmAddressByteString(), PreviousDeltaDfsHash = previousHash2
                    },
                    Voter = voter1.GetKvmAddressByteString()
                }, true)
        };

        [TestCaseSource(nameof(FavouritesComparisonData))]
        public void FavouriteByHashComparer_should_differentiate_by_candidate_hash_and_voter_only(FavouritesTestData favouritesTestData)
        {
            var xHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(favouritesTestData.X);
            var yHashCode = FavouriteByHashAndVoterComparer.Default.GetHashCode(favouritesTestData.Y);
            if (xHashCode != 0 && yHashCode != 0)
            {
                xHashCode.Equals(yHashCode).Should().Be(favouritesTestData.ComparisonResult);
            }

            FavouriteByHashAndVoterComparer.Default.Equals(favouritesTestData.X, favouritesTestData.Y).Should().Be(favouritesTestData.ComparisonResult);
        }
    }
}
