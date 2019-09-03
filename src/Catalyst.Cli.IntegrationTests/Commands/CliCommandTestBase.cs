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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Autofac;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Config;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Xunit.Abstractions;

namespace Catalyst.Cli.IntegrationTests.Commands
{
    /// <summary>
    /// This test is the base to all other tests.  If the Cli cannot connect to a node then all other commands
    /// will fail
    /// </summary>
    public abstract class CliCommandTestsBase : ConfigFileBasedTest
    {
        private protected static readonly string ServerNodeName = "node1";
        private protected static readonly string NodeArgumentPrefix = "-n";
        protected INodeRpcClient NodeRpcClient;
        protected ILifetimeScope Scope;
        protected ICatalystCli Shell;

        protected CliCommandTestsBase(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.ShellConfigFile)
        }, output)
        {
            ContainerProvider.ConfigureContainerBuilder();

            ConfigureNodeClient();

            CreateResolutionScope();

            ConnectShell();
        }

        private void CreateResolutionScope()
        {
            Scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
        }

        private void ConnectShell()
        {
            Shell = Scope.Resolve<ICatalystCli>();
            var hasConnected = Shell.ParseCommand("connect", NodeArgumentPrefix, ServerNodeName);
            hasConnected.Should().BeTrue();
        }

        protected void ConfigureNodeClient()
        {
            var channel = Substitute.For<IChannel>();
            channel.Active.Returns(true);

            NodeRpcClient = Substitute.For<INodeRpcClient>();
            NodeRpcClient.Channel.Returns(channel);
            NodeRpcClient.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));

            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            nodeRpcClientFactory
               .GetClient(Arg.Any<X509Certificate2>(), Arg.Is<IRpcNodeConfig>(c => c.NodeId == ServerNodeName))
               .Returns(NodeRpcClient);

            ContainerProvider.ContainerBuilder.RegisterInstance(nodeRpcClientFactory).As<INodeRpcClientFactory>();
        }

        protected T AssertSentMessageAndGetMessageContent<T>() where T : IMessage<T>
        {
            NodeRpcClient.Received(1).SendMessage(Arg.Is<IMessageDto<ProtocolMessage>>(x =>
                x.Content != null &&
                x.Content.GetType().IsAssignableTo<ProtocolMessage>() &&
                x.Content.FromProtocolMessage<T>() != null
            ));
            var sentMessageDto = (IMessageDto<ProtocolMessage>) NodeRpcClient.ReceivedCalls()
               .Single(c => c.GetMethodInfo().Name == nameof(INodeRpcClient.SendMessage))
               .GetArguments()[0];
            var requestSent = sentMessageDto.Content.FromProtocolMessage<T>();
            return requestSent;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Scope?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
