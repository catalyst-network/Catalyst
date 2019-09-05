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
using System.IO;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Protocol.UnitTests.Deltas
{
    public class FavouriteDeltaBroadcastTests
    {
        private class InvalidFavouriteDeltaBroadcasts : TheoryData<FavouriteDeltaBroadcast>
        {
            public InvalidFavouriteDeltaBroadcasts()
            {
                AddRow(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        ProducerId = null,
                        Hash = ByteString.CopyFromUtf8("hash"),
                        PreviousDeltaDfsHash = ByteString.CopyFromUtf8("yes")
                    },
                    VoterId = new PeerId()
                });
                AddRow(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        ProducerId = new PeerId(),
                        Hash = ByteString.Empty,
                        PreviousDeltaDfsHash = ByteString.CopyFromUtf8("yes")
                    },
                    VoterId = new PeerId()
                });
                AddRow(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        ProducerId = new PeerId(),
                        Hash = ByteString.CopyFromUtf8("hash"),
                        PreviousDeltaDfsHash = ByteString.Empty
                    },
                    VoterId = new PeerId()
                });
                AddRow(new FavouriteDeltaBroadcast
                {
                    Candidate = new CandidateDeltaBroadcast
                    {
                        ProducerId = new PeerId(),
                        Hash = ByteString.CopyFromUtf8("hash"),
                        PreviousDeltaDfsHash = ByteString.CopyFromUtf8("ok")
                    },
                    VoterId = null
                });
            }
        }


        [Theory]
        [ClassData(typeof(InvalidFavouriteDeltaBroadcasts))]
        public void FavouriteDeltaBroadcast_IsValid_Should_Throw_On_Invalid_FavouriteDeltaBroadcast(FavouriteDeltaBroadcast favourite)
        {
            new Action(() => favourite.IsValid()).Should().Throw<InvalidDataException>();
        }

        [Fact]
        public void FavouriteDeltaBroadcast_IsValid_Should_Not_Throw_On_Valid_FavouriteDeltaBroadcast()
        {
            var candidate = new FavouriteDeltaBroadcast
            {
                Candidate = new CandidateDeltaBroadcast
                {
                    ProducerId = new PeerId(),
                    Hash = ByteString.CopyFromUtf8("hash"),
                    PreviousDeltaDfsHash = ByteString.CopyFromUtf8("ok")
                },
                VoterId = new PeerId()
            };
            candidate.IsValid().Should().BeTrue();
        }
    }
}