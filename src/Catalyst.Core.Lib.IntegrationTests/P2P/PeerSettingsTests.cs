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
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.IntegrationTests.P2P
{
    public sealed class PeerSettingsTests : ConfigFileBasedTest
    {
        public PeerSettingsTests(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Testnet))
        }, output) { }

        [Fact]
        private void CanResolveIPeerSettings()
        {
            ContainerProvider.ConfigureContainerBuilder();

            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var peerDiscovery = scope.Resolve<IPeerSettings>();
                peerDiscovery.Network.Should().Be(Network.Testnet);
            }
        }
    }
}
