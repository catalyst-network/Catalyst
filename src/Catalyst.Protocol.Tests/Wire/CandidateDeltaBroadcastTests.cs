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
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Catalyst.Protocol.Tests.Wire
{
    public class CandidateDeltaTests
    {
        private sealed class InvalidCandidateDeltaBroadCasts : TheoryData<CandidateDeltaBroadcast>
        {
            public InvalidCandidateDeltaBroadCasts()
            {
                AddRow(new CandidateDeltaBroadcast
                {
                    ProducerId = null,
                    Hash = ByteString.CopyFromUtf8("hash"),
                    PreviousDeltaDfsHash = ByteString.CopyFromUtf8("yes")
                });
                AddRow(new CandidateDeltaBroadcast
                {
                    ProducerId = new PeerId(),
                    Hash = ByteString.Empty,
                    PreviousDeltaDfsHash = ByteString.CopyFromUtf8("yes")
                });
                AddRow(new CandidateDeltaBroadcast
                {
                    ProducerId = new PeerId(),
                    Hash = ByteString.CopyFromUtf8("yes"),
                    PreviousDeltaDfsHash = ByteString.Empty
                });
            }
        }

        [Theory]
        [ClassData(typeof(InvalidCandidateDeltaBroadCasts))]
        public void CandidateDeltaBroadcast_IsValid_Should_Throw_On_Invalid_CandidateDeltaBroadcast(CandidateDeltaBroadcast candidate)
        {
            new Action(() => candidate.IsValid()).Should().Throw<InvalidDataException>();
        }

        [Fact]
        public void CandidateDeltaBroadcast_IsValid_Should_Not_Throw_On_Valid_CandidateDeltaBroadcast()
        {
            var candidate = new CandidateDeltaBroadcast
            {
                ProducerId = new PeerId(),
                Hash = ByteString.CopyFromUtf8("yes"),
                PreviousDeltaDfsHash = ByteString.CopyFromUtf8("bla")
            };
            AssertionExtensions.Should((bool) candidate.IsValid()).BeTrue();
        }
    }
}
