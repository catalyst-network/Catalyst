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
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Rpc;
using Catalyst.Cli.Commands;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Cli.UnitTests
{
    public sealed class CommandContextTests
    {
        public CommandContextTests()
        {
            var configRoot = Substitute.For<IConfigurationRoot>();
            var logger = Substitute.For<ILogger>();
            var userOutput = Substitute.For<IUserOutput>();
            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            var certificateStore = Substitute.For<ICertificateStore>();
            var keyRegistry = Substitute.For<IKeyRegistry>();

            var cliSettings = configRoot.GetSection("CatalystCliConfig");
            cliSettings.GetSection("PublicKey").Value
               .Returns("9TEJQF7Y6Z31RB7XBPDYZT1ACPEK9BEC7N8R1E41GNZXT85RX20G");
            cliSettings.GetSection("BindAddress").Value.Returns("127.0.0.1");
            cliSettings.GetSection("Port").Value.Returns("5632");

            _commandContext = new CommandContext(configRoot, logger, userOutput,
                nodeRpcClientFactory, certificateStore, keyRegistry);
        }

        private readonly ICommandContext _commandContext;

        [Fact]
        public void GetConnectedNode_Should_Throw_ArgumentException_On_Empty_NodeId()
        {
            var exception = Record.Exception(() => _commandContext.GetConnectedNode(string.Empty));
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void GetConnectedNode_Should_Throw_ArgumentNullException_On_Null_NodeId()
        {
            var exception = Record.Exception(() => _commandContext.GetConnectedNode(null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void GetNodeConfig_Should_Return_Null_On_EmptyConfigs()
        {
            _commandContext.GetNodeConfig("No_node_config").Should().BeNull();
        }

        [Fact]
        public void GetNodeConfig_Should_Throw_ArgumentException_On_Empty_NodeId()
        {
            var exception = Record.Exception(() => _commandContext.GetNodeConfig(string.Empty));
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void GetNodeConfig_Should_Throw_ArgumentNullException_On_Null_NodeId()
        {
            var exception = Record.Exception(() => _commandContext.GetNodeConfig(null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void IsSocketChannelActive_Should_Return_False_On_Inactive_Channel()
        {
            var rpcClient = Substitute.For<INodeRpcClient>();
            rpcClient.Channel.Active.Returns(false);

            _commandContext.IsSocketChannelActive(rpcClient).Should().BeFalse();
        }

        [Fact]
        public void IsSocketChannelActive_Should_Return_True_On_Active_Channel()
        {
            var rpcClient = Substitute.For<INodeRpcClient>();
            rpcClient.Channel.Active.Returns(true);

            _commandContext.IsSocketChannelActive(rpcClient).Should().BeTrue();
        }
    }
}
