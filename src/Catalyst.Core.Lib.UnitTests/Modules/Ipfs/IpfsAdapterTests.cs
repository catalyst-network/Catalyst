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
using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Core.Lib.Modules.Dfs;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.UnitTests.Modules.Ipfs
{
    public sealed class IpfsAdapterTests : FileSystemBasedTest
    {
        private readonly IPeerSettings _peerSettings;
        private readonly IPasswordReader _passwordReader;
        private readonly ILogger _logger;

        public IpfsAdapterTests(ITestOutputHelper output) : base(output)
        {
            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.SeedServers.Returns(new[] {"seed1.server.va", "island.domain.tv"});
            _passwordReader = Substitute.For<IPasswordReader>();
            _passwordReader.ReadSecurePassword().ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("abcd"));
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void Constructor_should_read_seed_servers_addresses_from_peerSettings()
        {
            using (var ipfs = new IpfsAdapter(_passwordReader, _peerSettings, FileSystem, _logger))
            {
                ipfs.Options.Discovery.BootstrapPeers.Count().Should().Be(2);
            }
        }

        [Fact]
        public void Constructor_should_throw_when_no_peers_in_peerSettings()
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.SeedServers.Returns(new string[] { });
            new Action(() => new IpfsAdapter(_passwordReader, peerSettings, FileSystem, _logger))
               .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_read_a_password()
        {
            using (new IpfsAdapter(_passwordReader, _peerSettings, FileSystem, _logger))
            {
                _passwordReader.ReceivedWithAnyArgs(1).ReadSecurePassword();
            }
        }

        [Fact]
        public void Constructor_should_use_ipfs_subfolder()
        {
            using (var ipfs = new IpfsAdapter(_passwordReader, _peerSettings, FileSystem, _logger))
            {
                ipfs.Options.Repository.Folder.Should()
                   .Be(Path.Combine(FileSystem.GetCatalystDataDir().FullName, Constants.DfsDataSubDir));
            }
        }

        [Fact]
        public void Constructor_should_use_ipfs_private_network()
        {
            using (var ipfs = new IpfsAdapter(_passwordReader, _peerSettings, FileSystem, _logger))
            {
                ipfs.Options.Swarm.PrivateNetworkKey.Should().NotBeNull();
            }
        }
    }
}
