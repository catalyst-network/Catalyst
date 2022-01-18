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

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Rpc.Server.Tests.UnitTests
{
    public class RpcServerSettingsTests
    {
        [Test]
        public void Constructor_Should_Set_Settings_From_Config()
        {
            const string pfxFileName = "pfx";

            var config = Substitute.For<IConfigurationRoot>();
            var rpcSection = config.GetSection("CatalystNodeConfiguration").GetSection("Rpc");
            rpcSection.GetSection("PfxFileName").Value.Returns(pfxFileName);
            rpcSection.GetSection("Address").Value.Returns("/ip4/127.0.0.1/tcp/9000");

            RpcServerSettings rpcSeverSettings = new(config);

            rpcSeverSettings.NodeConfig.Should().Be(config);
            rpcSeverSettings.PfxFileName.Should().Be(pfxFileName);
            rpcSeverSettings.Address.ToString().Should().Be("/ip4/127.0.0.1/tcp/9000");
        }
    }
}
