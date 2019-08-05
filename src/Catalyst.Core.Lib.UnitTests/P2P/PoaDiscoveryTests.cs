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

using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.P2P;
using Catalyst.Core.Lib.P2P;
using Catalyst.TestUtils;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Multiformats.Base;
using Multiformats.Hash.Algorithms;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.UnitTests.P2P
{
    public sealed class PoaDiscoveryTests : FileSystemBasedTest
    {
        public PoaDiscoveryTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Can_Populate_Peers_Correctly()
        {
            var peerRepository = Substitute.For<IPeerRepository>();
            var pubkey = "hello".ComputeUtf8Multihash(new ID()).ToString(MultibaseEncoding.Base58Btc);

            var peers = new[]
            {
                new PoaPeer {Ip = "92.207.178.198", Port = 42069, PublicKey = pubkey},
                new PoaPeer {Ip = "127.0.0.1", Port = 42069, PublicKey = pubkey},
            };

            await FileSystem.WriteTextFileToCddAsync(PoaDiscovery.PoaPeerFile, JsonConvert.SerializeObject(peers));

            var peerDiscovery = new PoaDiscovery(peerRepository, FileSystem, Substitute.For<ILogger>());
            await peerDiscovery.DiscoveryAsync().ConfigureAwait(false);
            peerRepository.Received(peers.Length).Add(Arg.Any<Peer>());
        }
    }
}
