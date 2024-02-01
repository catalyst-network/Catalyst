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

using System.Threading.Tasks;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Modules.POA.P2P.Discovery;
using Catalyst.TestUtils;
using MultiFormats.Registry;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Abstractions.P2P;

namespace Catalyst.Modules.POA.P2P.Tests.UnitTests
{
    public sealed class PoaDiscoveryTests : FileSystemBasedTest
    {
        private IHashProvider _hashProvider;

        [SetUp]
        public void Init()
        {
            this.Setup(TestContext.CurrentContext);
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
        }

        [Test]
        public async Task Can_Populate_Peers_Correctly()
        {
            var peerRepository = Substitute.For<IPeerRepository>();
            var pubkey = _hashProvider.ComputeUtf8MultiHash("hello").ToBase32();

            var peers = new[]
            {
                new PoaPeer {Ip = "92.207.178.198", Port = 42069, PublicKey = pubkey},
                new PoaPeer {Ip = "127.0.0.1", Port = 42069, PublicKey = pubkey}
            };

            await FileSystem.WriteTextFileToCddAsync(PoaDiscovery.PoaPeerFile, JsonConvert.SerializeObject(peers));

            var peerDiscovery = new PoaDiscovery(Substitute.For<IPeerSettings>(), peerRepository, FileSystem, Substitute.For<ILogger>());
            await peerDiscovery.DiscoveryAsync().ConfigureAwait(false);
            peerRepository.Received(peers.Length).Add(Arg.Any<Peer>());
        }
    }
}
