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
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Abstractions.Cli.CommandTypes;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Transport;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Transport;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using Google.Protobuf;
using NSubstitute;

namespace Catalyst.Cli.UnitTests.Helpers
{
    public static class TestCommandHelpers
    {
        public static ICommandContext GenerateCliRequestCommandContext()
        {
            var commandContext = GenerateCliFullCommandContext();
            return commandContext;
        }

        public static ICommandContext GenerateCliFullCommandContext()
        {
            var userOutput = Substitute.For<IUserOutput>();
            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            var certificateStore = Substitute.For<ICertificateStore>();

            var commandContext = Substitute.For<ICommandContext>();
            commandContext.UserOutput.Returns(userOutput);
            commandContext.NodeRpcClientFactory.Returns(nodeRpcClientFactory);
            commandContext.CertificateStore.Returns(certificateStore);

            commandContext.PeerIdentifier.Returns(
                PeerIdentifierHelper.GetPeerIdentifier("9TEJQF7Y6Z31RB7XBPDYZT1ACPEK9BEC7N8R1E41GNZXT85RX20G", IPAddress.Any, 9010));

            var nodeRpcClient = MockNodeRpcClient();
            MockRpcNodeConfig(commandContext);
            MockNodeRpcClientFactory(commandContext, nodeRpcClient);
            MockActiveConnection(commandContext, nodeRpcClient);

            return commandContext;
        }

        public static ICommandContext GenerateCliCommandContext()
        {
            var userOutput = Substitute.For<IUserOutput>();
            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            var certificateStore = Substitute.For<ICertificateStore>();

            var commandContext = Substitute.For<ICommandContext>();
            commandContext.UserOutput.Returns(userOutput);
            commandContext.NodeRpcClientFactory.Returns(nodeRpcClientFactory);
            commandContext.CertificateStore.Returns(certificateStore);

            var hashingAlgorithm = Constants.HashAlgorithm;
            var deltaMultiHash = Encoding.UTF8.GetBytes("previous").ComputeMultihash(hashingAlgorithm);
            commandContext.PeerIdentifier.Returns(
                PeerIdentifierHelper.GetPeerIdentifier(deltaMultiHash, IPAddress.Any, 9010));

            return commandContext;
        }

        public static IRpcNodeConfig MockRpcNodeConfig(ICommandContext commandContext)
        {
            var rpcNodeConfig = Substitute.For<IRpcNodeConfig>();
            rpcNodeConfig.NodeId = "test";
            rpcNodeConfig.HostAddress = IPAddress.Any;
            rpcNodeConfig.PublicKey = "9TEJQF7Y6Z31RB7XBPDYZT1ACPEK9BEC7N8R1E41GNZXT85RX20G";
            rpcNodeConfig.Port = 9000;
            commandContext.GetNodeConfig(Arg.Any<string>()).Returns(rpcNodeConfig);
            return rpcNodeConfig;
        }

        public static INodeRpcClientFactory MockNodeRpcClientFactory(ICommandContext commandContext,
            INodeRpcClient nodeRpcClient)
        {
            commandContext.NodeRpcClientFactory.GetClient(Arg.Any<X509Certificate2>(), Arg.Any<IRpcNodeConfig>())
               .Returns(nodeRpcClient);
            return commandContext.NodeRpcClientFactory;
        }

        public static INodeRpcClient MockNodeRpcClient()
        {
            var nodeRpcClient = Substitute.For<INodeRpcClient>();
            nodeRpcClient.Channel.Active.Returns(true);
            nodeRpcClient.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));
            return nodeRpcClient;
        }

        public static void MockActiveConnection(ICommandContext commandContext, INodeRpcClient nodeRpcClient)
        {
            commandContext.IsSocketChannelActive(Arg.Any<INodeRpcClient>()).Returns(true);
            commandContext.GetConnectedNode(Arg.Any<string>()).Returns(nodeRpcClient);
        }

        public static ISocketClientRegistry<INodeRpcClient> AddClientSocketRegistry(ICommandContext commandContext,
            IScheduler testScheduler)
        {
            commandContext.SocketClientRegistry.Returns(new SocketClientRegistry<INodeRpcClient>(testScheduler));
            return commandContext.SocketClientRegistry;
        }

        public static void GenerateRequest(ICommandContext commandContext,
            ICommand command,
            params string[] commandArgs)
        {
            var commands = new List<ICommand> {command};
            var console = new CatalystCli(commandContext.UserOutput, commands);
            commandArgs = commandArgs.ToList().Prepend(command.CommandName).ToArray();
            console.ParseCommand(commandArgs);
        }

        public static ICommandContext GenerateCliResponseCommandContext(IScheduler testScheduler)
        {
            var userOutput = Substitute.For<IUserOutput>();
            var clientSocketRegistry = new SocketClientRegistry<INodeRpcClient>(testScheduler);
            var commandContext = Substitute.For<ICommandContext>();
            commandContext.SocketClientRegistry.Returns(clientSocketRegistry);
            commandContext.UserOutput.Returns(userOutput);
            return commandContext;
        }

        private static INodeRpcClient GenerateRpcResponseOnSubscription<T>(T response) where T : IMessage<T>
        {
            var socket = Substitute.For<INodeRpcClient>();
            socket.Channel.Active.Returns(true);
            socket.SubscribeToResponse(Arg.Invoke(response));
            return socket;
        }

        public static void GenerateResponse<T>(ICommandContext commandContext, T response) where T : IMessage<T>
        {
            var nodeRpcClient = GenerateRpcResponseOnSubscription(response);
            commandContext.SocketClientRegistry.AddClientToRegistry(1111111111, nodeRpcClient);
        }

        public static T GetRequest<T>(INodeRpcClient connectedNode) where T : IMessage<T>
        {
            connectedNode.Received(1).SendMessage(Arg.Any<IMessageDto<ProtocolMessage>>());

            var sentMessageDto = (IMessageDto<ProtocolMessage>) connectedNode.ReceivedCalls()
               .Single(c => c.GetMethodInfo().Name == nameof(INodeRpcClient.SendMessage))
               .GetArguments()[0];

            return sentMessageDto.Content.FromProtocolMessage<T>();
        }
    }
}
