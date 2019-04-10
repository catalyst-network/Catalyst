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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Cli;
using Catalyst.Cli.Rpc;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public sealed class SocketTests : ConfigFileBasedTest
    {
        private const int MaxWaitInMs = 1000;
        private readonly IConfigurationRoot _config;

        private readonly INodeRpcClientFactory _nodeRpcClientFactory;
        private IRpcServer _rpcServer;
        private ICertificateStore _certificateStore;
        private NodeRpcClient _nodeRpcClient;
        private ILifetimeScope _scope;
        private ILogger _logger;

        public SocketTests(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellConfigFile))
               .Build(), CurrentTestName);

            var mempool = Substitute.For<IMempool>();
            mempool.GetMemPoolContentEncoded().Returns(x =>
                {
                    List<Transaction> txLst = new List<Transaction>
                    {
                        TransactionHelper.GetTransaction(234, "standardPubKey", "sign1"),
                        TransactionHelper.GetTransaction(567, "standardPubKey", "sign2")
                    };

                    List<byte[]> txEncodedLst = txLst.Select(tx => tx.ToString().ToBytesForRLPEncoding()).ToList();
                    return txEncodedLst;
                }
            );

            ConfigureContainerBuilder(_config);

            // register fact test certioficate store
            ContainerBuilder.RegisterInstance(mempool).As<IMempool>();

            var container = ContainerBuilder.Build();

            _scope = container.BeginLifetimeScope(CurrentTestName);

            _logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider(_logger));

            //resolve here to oiverride
            _certificateStore = container.Resolve<ICertificateStore>();

            _rpcServer = container.Resolve<IRpcServer>();
            _nodeRpcClientFactory = container.Resolve<INodeRpcClientFactory>();
        }

        [Fact]
        public void ServerConnectedToCorrectPort()
        {
            using (var client = new TcpClient(_rpcServer.Settings.BindAddress.ToString(),
                _rpcServer.Settings.Port))
            {
                client.Should().NotBeNull();
                client.Connected.Should().BeTrue();
            }
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcServer_Can_Handle_GetInfoRequest()
        {
            // _nodeRpcClient = new NodeRpcClient(_certificateStore.ReadOrCreateCertificateFile("mycert.pfx"), NodeRpcConfig.BuildRpcNodeSettingList(_config));
            // _nodeRpcClient.Should().NotBeNull();

            var shell = new Shell(_nodeRpcClientFactory, _config, _logger, _certificateStore);
            var hasConnected = shell.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var serverObserver = new AnySignedMessageObserver(0, _logger);
            var clientObserver = new AnySignedMessageObserver(1, _logger);
            using (_rpcServer.MessageStream.Subscribe(serverObserver))
            using (_nodeRpcClient.MessageStream.Subscribe(clientObserver))
            {
                var info = shell.ParseCommand("get", "-i", "node1");

                var tasks = new IChanneledMessageStreamer<AnySigned>[]
                    {
                        _nodeRpcClient, _rpcServer
                    }
                   .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                   .ToArray();
                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(MaxWaitInMs));

                serverObserver.Received.Should().NotBeNull();
                serverObserver.Received.Payload.TypeUrl.Should().Be(GetInfoRequest.Descriptor.ShortenedFullName());

                clientObserver.Received.Should().NotBeNull();
                clientObserver.Received.Payload.TypeUrl.Should().Be(GetInfoResponse.Descriptor.ShortenedFullName());
            }
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcServer_Can_Handle_GetVersionRequest()
        {
            // _nodeRpcClient = new NodeRpcClient(_logger, _certificateStore);
            // _nodeRpcClient.Should().NotBeNull();

            var shell = new Shell(_nodeRpcClientFactory, _config, _logger, _certificateStore);
            var hasConnected = shell.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var serverObserver = new AnySignedMessageObserver(0, _logger);
            var clientObserver = new AnySignedMessageObserver(1, _logger);
            using (_rpcServer.MessageStream.Subscribe(serverObserver))
            using (_nodeRpcClient.MessageStream.Subscribe(clientObserver))
            {
                var info = shell.ParseCommand("get", "-v", "node1");

                var tasks = new IChanneledMessageStreamer<AnySigned>[]
                    {
                        _nodeRpcClient, _rpcServer
                    }
                   .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                   .ToArray();
                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(MaxWaitInMs));

                serverObserver.Received.Should().NotBeNull();
                serverObserver.Received.Payload.TypeUrl.Should().Be(VersionRequest.Descriptor.ShortenedFullName());

                clientObserver.Received.Should().NotBeNull();
                clientObserver.Received.Payload.TypeUrl.Should().Be(VersionResponse.Descriptor.ShortenedFullName());
            }
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcServer_Can_Handle_GetMempoolRequest()
        {
            // _nodeRpcClient = new NodeRpcClient(_logger, _certificateStore);
            // _nodeRpcClient.Should().NotBeNull();

            var shell = new Shell(_nodeRpcClientFactory, _config, _logger, _certificateStore);
            var hasConnected = shell.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var serverObserver = new AnySignedMessageObserver(0, _logger);
            var clientObserver = new AnySignedMessageObserver(1, _logger);
            using (_rpcServer.MessageStream.Subscribe(serverObserver))
            using (_nodeRpcClient.MessageStream.Subscribe(clientObserver))
            {
                var info = shell.ParseCommand("get", "-m", "node1");

                var tasks = new IChanneledMessageStreamer<AnySigned>[]
                    {
                        _nodeRpcClient, _rpcServer
                    }
                   .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                   .ToArray();
                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(MaxWaitInMs));

                serverObserver.Received.Should().NotBeNull();
                serverObserver.Received.Payload.TypeUrl.Should().Be(GetMempoolRequest.Descriptor.ShortenedFullName());

                clientObserver.Received.Should().NotBeNull();
                clientObserver.Received.Payload.TypeUrl.Should().Be(GetMempoolResponse.Descriptor.ShortenedFullName());
            }
        }

        [Fact]
        public void RpcServer_Can_Handle_SignMessageRequest()
        {
            var shell = new Shell(_nodeRpcClientFactory, _config, _logger, _certificateStore);
            var hasConnected = shell.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var serverObserver = new AnySignedMessageObserver(0, _logger);
            var clientObserver = new AnySignedMessageObserver(1, _logger);
            using (_rpcServer.MessageStream.Subscribe(serverObserver))
            using (_rpcClient.MessageStream.Subscribe(clientObserver))
            {
                var info = shell.ParseCommand("sign", "-m", "Hello Catalyst", "-n", "node1");

                var tasks = new IChanneledMessageStreamer<AnySigned>[]
                    {
                        _rpcClient, _rpcServer
                    }
                   .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                   .ToArray();
                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(MaxWaitInMs));

                serverObserver.Received.Should().NotBeNull();
                serverObserver.Received.Payload.TypeUrl.Should().Be(SignMessageRequest.Descriptor.ShortenedFullName());

                clientObserver.Received.Should().NotBeNull();
                clientObserver.Received.Payload.TypeUrl.Should().Be(SignMessageResponse.Descriptor.ShortenedFullName());
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            _scope?.Dispose();
            _rpcServer?.Dispose();
            _nodeRpcClient?.Dispose();
        }
    }
}
