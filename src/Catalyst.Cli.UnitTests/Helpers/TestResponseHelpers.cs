using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using Catalyst.Cli.Commands;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Transport;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;

namespace Catalyst.Cli.UnitTests.Helpers
{
    public static class TestResponseHelpers
    {
        private static ICommandContext GenerateCommandContext(IScheduler testScheduler)
        {
            var config = Substitute.For<IConfigurationRoot>();
            var logger = Substitute.For<ILogger>();
            var userOutput = Substitute.For<IUserOutput>();
            var peerIdClientId = Substitute.For<IPeerIdClientId>();
            var dtoFactory = Substitute.For<IDtoFactory>();
            var nodeRpcFactory = Substitute.For<INodeRpcClientFactory>();
            var certificateStore = Substitute.For<ICertificateStore>();
            var clientSocketRegistry = new SocketClientRegistry<INodeRpcClient>(testScheduler);
            var commandContext = new CommandContext(config, logger, userOutput, peerIdClientId, dtoFactory, nodeRpcFactory, certificateStore, clientSocketRegistry);
            return commandContext;
        }

        private static INodeRpcClient GenerateRpcResponseOnSubscription<T>(T response) where T : IMessage<T>
        {
            var socket = Substitute.For<INodeRpcClient>();
            socket.Channel.Active.Returns(true);
            socket.SubscribeToResponse(Arg.Invoke(response));
            return socket;
        }

        public static ICommandContext GenerateResponse<T>(TestScheduler testScheduler, T response) where T : IMessage<T>
        {
            var commandContext = GenerateCommandContext(testScheduler);
            var nodeRpcClient = GenerateRpcResponseOnSubscription(response);
            commandContext.SocketClientRegistry.AddClientToRegistry(1111111111, nodeRpcClient);
            return commandContext;
        }
    }
}
