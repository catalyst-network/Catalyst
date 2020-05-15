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
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NUnit.Framework;
using System.Collections.Generic;
using CandidateDeltaBroadcast = Catalyst.Protocol.Wire.CandidateDeltaBroadcast;

namespace Catalyst.Protocol.Tests.UnitTests.Wire
{
    public sealed class FavouriteDeltaBroadcastTests
    {
        private sealed class InvalidFavouriteDeltaBroadcasts : List<FavouriteDeltaBroadcast>
        {
            public InvalidFavouriteDeltaBroadcasts()
            {
                Add(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        ProducerId = "",
                        Hash = ByteString.CopyFromUtf8("hash"),
                        PreviousDeltaDfsHash = ByteString.CopyFromUtf8("yes")
                    },
                    VoterId = new PeerId().ToString()
                });
                Add(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        ProducerId = new PeerId().ToString(),
                        Hash = ByteString.Empty,
                        PreviousDeltaDfsHash = ByteString.CopyFromUtf8("yes")
                    },
                    VoterId = new PeerId().ToString()
                });
                Add(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        ProducerId = new PeerId().ToString(),
                        Hash = ByteString.CopyFromUtf8("hash"),
                        PreviousDeltaDfsHash = ByteString.Empty
                    },
                    VoterId = new PeerId().ToString()
                });
                Add(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        ProducerId = new PeerId().ToString(),
                        Hash = ByteString.CopyFromUtf8("hash"),
                        PreviousDeltaDfsHash = ByteString.CopyFromUtf8("ok")
                    },
                    VoterId = ""
                });
            }
        }

        [TestCaseSource(typeof(InvalidFavouriteDeltaBroadcasts))]
        public void FavouriteDeltaBroadcast_IsValid_Should_Throw_On_Invalid_FavouriteDeltaBroadcast(FavouriteDeltaBroadcast favourite)
        {
            favourite.IsValid().Should().BeFalse();
        }

        [Test]
        public void FavouriteDeltaBroadcast_IsValid_Should_Not_Throw_On_Valid_FavouriteDeltaBroadcast()
        {
            var candidate = new FavouriteDeltaBroadcast
            {
                Candidate = new CandidateDeltaBroadcast
                {
                    ProducerId = PeerIdHelper.GetPeerId("producer").ToString(),
                    Hash = ByteString.CopyFromUtf8("hash"),
                    PreviousDeltaDfsHash = ByteString.CopyFromUtf8("ok")
                },
                VoterId = PeerIdHelper.GetPeerId("voter").ToString(),
            };
            candidate.IsValid().Should().BeTrue();
        }
    }
}
