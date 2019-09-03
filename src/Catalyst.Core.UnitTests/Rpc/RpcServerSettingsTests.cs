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

using System.Net;
using Catalyst.Core.Rpc;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc
{
    public class RpcServerSettingsTests
    {
        [Fact]
        public void Constructor_Should_Set_Settings_From_Config()
        {
            const string pfxFileName = "pfx";
            const int port = 9000;
            var bindAddress = IPAddress.Loopback.ToString();

            var config = Substitute.For<IConfigurationRoot>();
            var rpcSection = config.GetSection("CatalystNodeConfiguration").GetSection("Rpc");
            rpcSection.GetSection("Port").Value.Returns(port.ToString());
            rpcSection.GetSection("PfxFileName").Value.Returns(pfxFileName);
            rpcSection.GetSection("BindAddress").Value.Returns(bindAddress);

            var rpcSeverSettings = new RpcServerSettings(config);

            rpcSeverSettings.NodeConfig.Should().Be(config);
            rpcSeverSettings.PfxFileName.Should().Be(pfxFileName);
            rpcSeverSettings.Port.Should().Be(port);
            rpcSeverSettings.BindAddress.Should().Be(IPAddress.Parse(bindAddress));
        }
    }
}
