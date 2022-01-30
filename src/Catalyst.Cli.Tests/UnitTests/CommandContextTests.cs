#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Cli.Commands;
using Catalyst.Modules.Network.Dotnetty.Abstractions.Cli.Commands;
using Catalyst.Modules.Network.Dotnetty.IO.Transport;
using Catalyst.Modules.Network.Dotnetty.Rpc;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Catalyst.Cli.Tests.UnitTests
{
    public sealed class CommandContextTests
    {
        public CommandContextTests()
        {
            var configRoot = Substitute.For<IConfigurationRoot>();
            var logger = Substitute.For<ILogger>();
            var userOutput = Substitute.For<IUserOutput>();
            var nodeRpcClientFactory = Substitute.For<IRpcClientFactory>();
            var certificateStore = Substitute.For<ICertificateStore>();

            var cliSettings = configRoot.GetSection("CatalystCliConfig");
            cliSettings.GetSection("PublicKey").Value
               .Returns("hv6vvbt2u567syz5labuqnfabsc3zobfwekl4cy3c574n6vkj7sq");
            cliSettings.GetSection("BindAddress").Value.Returns("127.0.0.1");
            cliSettings.GetSection("Port").Value.Returns("5632");

            var clientRegistry = new SocketClientRegistry<IRpcClient>();

            _commandContext = new CommandContext(configRoot, logger, userOutput,
                nodeRpcClientFactory, certificateStore, clientRegistry);
        }

        private readonly ICommandContext _commandContext;

        [Test]
        public void GetConnectedNode_Should_Throw_ArgumentException_On_Empty_NodeId()
        {
            Assert.Throws<ArgumentException>(() => _commandContext.GetConnectedNode(string.Empty));
        }

        [Test]
        public void GetConnectedNode_Should_Throw_ArgumentNullException_On_Null_NodeId()
        {
            Assert.Throws<ArgumentNullException>(() => _commandContext.GetConnectedNode(null));
        }

        [Test]
        public void GetNodeConfig_Should_Return_Null_On_EmptyConfigs()
        {
            _commandContext.GetNodeConfig("No_node_config").Should().BeNull();
        }

        [Test]
        public void GetNodeConfig_Should_Throw_ArgumentException_On_Empty_NodeId()
        {
            Assert.Throws<ArgumentException>(() => _commandContext.GetNodeConfig(string.Empty));
        }

        [Test]
        public void GetNodeConfig_Should_Throw_ArgumentNullException_On_Null_NodeId()
        {
            Assert.Throws<ArgumentNullException>(() => _commandContext.GetNodeConfig(null));
        }

        [Test]
        public void IsSocketChannelActive_Should_Return_False_On_Inactive_Channel()
        {
            var rpcClient = Substitute.For<IRpcClient>();
            rpcClient.Channel.Active.Returns(false);

            _commandContext.IsSocketChannelActive(rpcClient).Should().BeFalse();
        }

        [Test]
        public void IsSocketChannelActive_Should_Return_True_On_Active_Channel()
        {
            var rpcClient = Substitute.For<IRpcClient>();
            rpcClient.Channel.Active.Returns(true);

            _commandContext.IsSocketChannelActive(rpcClient).Should().BeTrue();
        }
    }
}
