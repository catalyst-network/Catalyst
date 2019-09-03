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

using System.IO;
using System.Linq;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Catalyst.Core.Dfs;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.UnitTests.Dfs
{
    public sealed class IpfsAdapterTests : FileSystemBasedTest
    {
        private readonly IPasswordManager _passwordManager;
        private readonly ILogger _logger;

        public IpfsAdapterTests(ITestOutputHelper output) : base(output)
        {
            _passwordManager = Substitute.For<IPasswordManager>();
            _passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.IpfsPassword,
                Arg.Any<string>()).ReturnsForAnyArgs(
                TestPasswordReader.BuildSecureStringPassword("abcd"));
            
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void Constructor_should_read_seed_servers_addresses_from_peerSettings()
        {
            using (var ipfs = new IpfsAdapter(_passwordManager, FileSystem, _logger))
            {
                ipfs.Options.Discovery.BootstrapPeers.Count().Should().Be(8);
            }
        }

        [Fact]
        public void Constructor_should_read_a_password()
        {
            using (new IpfsAdapter(_passwordManager, FileSystem, _logger))
            {
                _passwordManager.ReceivedWithAnyArgs(1)
                   .RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.IpfsPassword);
            }
        }

        [Fact]
        public void Constructor_should_use_ipfs_subfolder()
        {
            using (var ipfs = new IpfsAdapter(_passwordManager, FileSystem, _logger))
            {
                ipfs.Options.Repository.Folder.Should()
                   .Be(Path.Combine(FileSystem.GetCatalystDataDir().FullName, Constants.DfsDataSubDir));
            }
        }

        [Fact]
        public void Constructor_should_use_ipfs_private_network()
        {
            using (var ipfs = new IpfsAdapter(_passwordManager, FileSystem, _logger))
            {
                ipfs.Options.Swarm.PrivateNetworkKey.Should().NotBeNull();
            }
        }
    }
}
