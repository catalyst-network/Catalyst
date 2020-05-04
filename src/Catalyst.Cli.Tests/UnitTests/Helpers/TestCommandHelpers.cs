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
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Abstractions.Cli.CommandTypes;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Transport;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Transport;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using Google.Protobuf;
using MultiFormats.Registry;
using NSubstitute;

namespace Catalyst.Cli.Tests.UnitTests.Helpers
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
            var nodeRpcClientFactory = Substitute.For<IRpcClientFactory>();
            var certificateStore = Substitute.For<ICertificateStore>();

            var commandContext = Substitute.For<ICommandContext>();
            commandContext.UserOutput.Returns(userOutput);
            commandContext.RpcClientFactory.Returns(nodeRpcClientFactory);
            commandContext.CertificateStore.Returns(certificateStore);

            commandContext.PeerId.Returns(
                "hv6vvbt2u567syz5labuqnfabsc3zobfwekl4cy3c574n6vkj7sq".BuildPeerIdFromBase32Key(IPAddress.Any, 9010));

            var nodeRpcClient = MockNodeRpcClient();
            MockRpcNodeConfig(commandContext);
            MockNodeRpcClientFactory(commandContext, nodeRpcClient);
            MockActiveConnection(commandContext, nodeRpcClient);

            return commandContext;
        }

        public static ICommandContext GenerateCliCommandContext()
        {
            var userOutput = Substitute.For<IUserOutput>();
            var nodeRpcClientFactory = Substitute.For<IRpcClientFactory>();
            var certificateStore = Substitute.For<ICertificateStore>();

            var commandContext = Substitute.For<ICommandContext>();
            commandContext.UserOutput.Returns(userOutput);
            commandContext.RpcClientFactory.Returns(nodeRpcClientFactory);
            commandContext.CertificateStore.Returns(certificateStore);

            IHashProvider hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            var deltaMultiHash = hashProvider.ComputeUtf8MultiHash("previous");
            commandContext.PeerId.Returns(
                PeerIdHelper.GetPeerId(deltaMultiHash.Digest, IPAddress.Any, 9010));

            return commandContext;
        }

        public static IRpcClientConfig MockRpcNodeConfig(ICommandContext commandContext)
        {
            var rpcNodeConfig = Substitute.For<IRpcClientConfig>();
            rpcNodeConfig.NodeId = "test";
            rpcNodeConfig.HostAddress = IPAddress.Any;
            rpcNodeConfig.PublicKey = "hv6vvbt2u567syz5labuqnfabsc3zobfwekl4cy3c574n6vkj7sq";
            rpcNodeConfig.Port = 9000;
            commandContext.GetNodeConfig(Arg.Any<string>()).Returns(rpcNodeConfig);
            return rpcNodeConfig;
        }

        public static IRpcClientFactory MockNodeRpcClientFactory(ICommandContext commandContext,
            IRpcClient rpcClient)
        {
            commandContext.RpcClientFactory.GetClientAsync(Arg.Any<X509Certificate2>(), Arg.Any<IRpcClientConfig>())
               .Returns(rpcClient);
            return commandContext.RpcClientFactory;
        }

        public static IRpcClient MockNodeRpcClient()
        {
            var nodeRpcClient = Substitute.For<IRpcClient>();
            nodeRpcClient.Channel.Active.Returns(true);
            nodeRpcClient.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));
            return nodeRpcClient;
        }

        public static void MockActiveConnection(ICommandContext commandContext, IRpcClient rpcClient)
        {
            commandContext.IsSocketChannelActive(Arg.Any<IRpcClient>()).Returns(true);
            commandContext.GetConnectedNode(Arg.Any<string>()).Returns(rpcClient);
        }

        public static ISocketClientRegistry<IRpcClient> AddClientSocketRegistry(ICommandContext commandContext,
            IScheduler testScheduler)
        {
            commandContext.SocketClientRegistry.Returns(new SocketClientRegistry<IRpcClient>(testScheduler));
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
            var clientSocketRegistry = new SocketClientRegistry<IRpcClient>(testScheduler);
            var commandContext = Substitute.For<ICommandContext>();
            commandContext.SocketClientRegistry.Returns(clientSocketRegistry);
            commandContext.UserOutput.Returns(userOutput);
            return commandContext;
        }

        private static IRpcClient GenerateRpcResponseOnSubscription<T>(T response) where T : IMessage<T>
        {
            var socket = Substitute.For<IRpcClient>();
            socket.Channel.Active.Returns(true);
            socket.SubscribeToResponse(Arg.Invoke(response));
            return socket;
        }

        public static void GenerateResponse<T>(ICommandContext commandContext, T response) where T : IMessage<T>
        {
            var nodeRpcClient = GenerateRpcResponseOnSubscription(response);
            commandContext.SocketClientRegistry.AddClientToRegistry(1111111111, nodeRpcClient);
        }

        public static T GetRequest<T>(IRpcClient connected) where T : IMessage<T>
        {
            connected.Received(1).SendMessage(Arg.Any<IMessageDto<ProtocolMessage>>());

            var sentMessageDto = (IMessageDto<ProtocolMessage>) connected.ReceivedCalls()
               .Single(c => c.GetMethodInfo().Name == nameof(IRpcClient.SendMessage))
               .GetArguments()[0];

            return sentMessageDto.Content.FromProtocolMessage<T>();
        }
    }
}
