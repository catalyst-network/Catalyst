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
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Cli.Tests.IntegrationTests.Commands
{
    /// <summary>
    /// This test is the base to all other tests.  If the Cli cannot connect to a node then all other commands
    /// will fail
    /// </summary>
    public abstract class CliCommandTestsBase : FileSystemBasedTest
    {
        private protected static readonly string ServerNodeName = "node1";
        private protected static readonly string NodeArgumentPrefix = "-n";
        protected IRpcClient RpcClient;
        protected ILifetimeScope Scope;
        protected ICatalystCli Shell;

        protected CliCommandTestsBase(TestContext output) : base(output, new[]
        {
            Path.Combine(Constants.ConfigSubFolder, TestConstants.TestShellNodesConfigFile),
            Path.Combine(Constants.ConfigSubFolder, CliConstants.ShellConfigFile),
        })
        {

        }

        public override void Setup(TestContext output)
        {
            base.Setup(output);

            ConfigureModules();

            ConfigureNodeClient();

            CreateResolutionScope();

            ConnectShell();
        }

        private void ConfigureModules()
        {
            var containerBuilder = ContainerProvider.ContainerBuilder;
            CatalystCliBase.RegisterClientDependencies(containerBuilder);
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

            RpcClient = Substitute.For<IRpcClient>();
            RpcClient.Channel.Returns(channel);
            RpcClient.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));

            var nodeRpcClientFactory = Substitute.For<IRpcClientFactory>();
            nodeRpcClientFactory
               .GetClientAsync(Arg.Any<X509Certificate2>(), Arg.Is<IRpcClientConfig>(c => c.NodeId == ServerNodeName))
               .Returns(RpcClient);

            ContainerProvider.ContainerBuilder.RegisterInstance(nodeRpcClientFactory).As<IRpcClientFactory>();
        }

        protected T AssertSentMessageAndGetMessageContent<T>() where T : IMessage<T>
        {
            RpcClient.Received(1).SendMessage(Arg.Is<IMessageDto<ProtocolMessage>>(x =>
                x.Content != null &&
                x.Content.GetType().IsAssignableTo<ProtocolMessage>() &&
                x.Content.FromProtocolMessage<T>() != null
            ));
            var sentMessageDto = (IMessageDto<ProtocolMessage>) RpcClient.ReceivedCalls()
               .Single(c => c.GetMethodInfo().Name == nameof(IRpcClient.SendMessage))
               .GetArguments()[0];
            var requestSent = sentMessageDto.Content.FromProtocolMessage<T>();
            return requestSent;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Scope?.Dispose();

            base.Dispose(disposing);
        }
    }
}
