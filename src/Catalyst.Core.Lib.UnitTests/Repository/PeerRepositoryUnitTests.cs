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
using System.Linq;
using System.Net;
using Catalyst.Common.P2P.Models;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using MongoDB.Bson;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.UnitTests.Repository
{
    public class PeerRepositoryUnitTests : SelfAwareTestBase
    {
        public PeerRepositoryUnitTests(ITestOutputHelper output) : base(output) { }

        [Fact(Skip = "Just ran as a one off for Stephen")]
        public void Can_Rely_on_Serialisers()
        {
            var random = new Random();

            var peers = Enumerable.Range(0, 10).Select(i =>
            {
                var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(
                    i.ToString(),
                    new IPAddress(ByteUtil.GenerateRandomByteArray(4)));
                var peer = new Peer
                {
                    PeerIdentifier = peerIdentifier,
                    Reputation = random.Next(100),
                };

                return peer;
            });

            var peerIds = peers.Select(p => p.PeerIdentifier.PeerId).ToArray();
            var output =
                JsonConvert.SerializeObject(peers, 
                    Formatting.Indented, new JsonProtoObjectConverter<PeerId>(), new IpEndPointConverter(), new IpAddressConverter());
            
            Output.WriteLine(output);
        }
    }
}

