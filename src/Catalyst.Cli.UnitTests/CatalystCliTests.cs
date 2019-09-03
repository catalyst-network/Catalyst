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

using System.Collections.Generic;
using System.Threading;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Abstractions.Cli.CommandTypes;
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
    public sealed class CatalystCliTests
    {
        public CatalystCliTests()
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
        public void ParseCommand_That_Does_Exist_Should_Return_True()
        {
            var userOutput = Substitute.For<IUserOutput>();
            var command = Substitute.For<ICommand>();
            command.CommandName.Returns("test");
            command.Parse(Arg.Any<string[]>()).Returns(true);

            var commands = new List<ICommand> {command};
            var catalystCli = new CatalystCli(userOutput, commands);
            catalystCli.ParseCommand("test").Should().BeTrue();
        }

        [Fact]
        public void ParseCommand_That_Does_Not_Exist_Should_Return_False()
        {
            var userOutput = Substitute.For<IUserOutput>();
            var command = new GetVersionCommand(_commandContext);
            var commands = new List<ICommand> {command};
            var catalystCli = new CatalystCli(userOutput, commands);
            catalystCli.ParseCommand("test").Should().BeFalse();
        }

        [Fact]
        public void RunConsole_Stops_On_Cancellation_Token()
        {
            var userOutput = Substitute.For<IUserOutput>();
            var catalystCli = new CatalystCli(userOutput, null);
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            cancellationTokenSource.Cancel();
            catalystCli.RunConsole(cancellationToken);
        }
    }
}
