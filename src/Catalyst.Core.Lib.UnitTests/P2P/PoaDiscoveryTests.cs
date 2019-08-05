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
using Catalyst.Common.P2P.Models;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.UnitTests.P2P
{
    public sealed class PoaDiscoveryTests : FileSystemBasedTest
    {
        public PoaDiscoveryTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("AC|01|92.207.178.198|42069|01234567890123456789222222222222", "AC|01|127.0.0.1|42069|01234567890123456789222222222222")]
        public async Task Can_Populate_Peers_Correctly(params string[] pids)
        {
            var peerRepository = Substitute.For<IPeerRepository>();
            await FileSystem.WriteTextFileToCddAsync(PoaDiscovery.PoaPeerFile, JsonConvert.SerializeObject(pids));

            var peerDiscovery = new PoaDiscovery(peerRepository, FileSystem, Substitute.For<ILogger>());
            await peerDiscovery.DiscoveryAsync().ConfigureAwait(false);
            peerRepository.Received(pids.Length).Add(Arg.Any<Peer>());
        }
    }
}
