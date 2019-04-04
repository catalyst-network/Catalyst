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
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Serilog.Extensions.Logging;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public class SocketTests : ConfigFileBasedTest, IDisposable
    {
        private readonly IConfigurationRoot _config;

        private IRpcServer _rpcServer;
        private ICertificateStore _certificateStore;
        private RpcClient _rpcClient;
        private ILifetimeScope _scope;
        private ILogger _logger;
        
        private readonly IMempool _memPool;
        private readonly Transaction _transaction;
        private readonly IRepository<Transaction, TransactionSignature> _transactionStore;

        public SocketTests(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build(), _currentTestName);

            WriteLogsToFile = false;
            WriteLogsToTestOutput = false;

            var mempool = Substitute.For<IMempool>();
            mempool.GetMemPoolContentEncoded().Returns(x =>
                {
                    List<Catalyst.Protocol.Transaction.Transaction> txLst = new List<Transaction>();

                    txLst.Add(TransactionHelper.GetTransaction(234, "standardPubKey", "sign1")); 
                    
                    txLst.Add(TransactionHelper.GetTransaction(567,"standardPubKey", "sign2"));

                    List<byte[]> txEncodedLst = txLst.Select(tx => tx.ToString().ToBytesForRLPEncoding()).ToList();
                    return txEncodedLst;
                }
                );

            ConfigureContainerBuilder(_config);
            ContainerBuilder.RegisterInstance(mempool).As<IMempool>();

            var container = ContainerBuilder.Build();

            _scope = container.BeginLifetimeScope(_currentTestName);

            _logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider(_logger));

            _certificateStore = container.Resolve<ICertificateStore>();
            _rpcServer = container.Resolve<IRpcServer>();
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

        [Fact(Skip = "trying to exclude P2P from the tests for now")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcServer_and_P2PServer_should_work_together()
        {
            _rpcClient = new RpcClient(_logger, _certificateStore);
            _rpcClient.Should().NotBeNull();

            var peerSettings = new PeerSettings(_config) {Port = _rpcServer.Settings.Port + 1000};
            var p2PMessenger = new P2PMessaging(peerSettings, _certificateStore, _logger);
            p2PMessenger.Should().NotBeNull();

            var shell = new Shell(_rpcClient, _config, _logger);
            var hasConnected = shell.OnCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");
            node1.SocketClient.Channel.Active.Should().BeTrue();
            node1.SocketClient.Shutdown();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcClient_can_send_request_and_RpcServer_can_reply()
        {
            _rpcClient = new RpcClient(_logger, _certificateStore);
            _rpcClient.Should().NotBeNull();

            var shell = new Shell(_rpcClient, _config, _logger);
            var hasConnected = shell.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var serverObserver = new AnyMessageObserver(0, _logger);
            var clientObserver = new AnyMessageObserver(1, _logger);
            using (_rpcServer.MessageStream.Subscribe(serverObserver))
            using (_rpcClient.MessageStream.Subscribe(clientObserver))
            {
                var info = shell.ParseCommand("get", "-i", "node1");

                var tasks = new IChanneledMessageStreamer<Any>[] { _rpcClient, _rpcServer }
                   .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAny))
                   .ToArray();
                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(500));

                serverObserver.Received.Should().NotBeNull();
                serverObserver.Received.Payload.TypeUrl.Should().Be(GetInfoRequest.Descriptor.ShortenedFullName());

                clientObserver.Received.Should().NotBeNull();
                clientObserver.Received.Payload.TypeUrl.Should().Be(GetInfoResponse.Descriptor.ShortenedFullName());
            }
        
        }
        
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcServer_Can_Handle_GetMempoolRequest()
        {
            _rpcClient = new RpcClient(_logger, _certificateStore);
            _rpcClient.Should().NotBeNull();

            var shell = new Shell(_rpcClient, _config, _logger);
            var hasConnected = shell.ParseCommand("connect", "-n", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var serverObserver = new AnyMessageObserver(0, _logger);
            var clientObserver = new AnyMessageObserver(1, _logger);
            using (_rpcServer.MessageStream.Subscribe(serverObserver))
            using (_rpcClient.MessageStream.Subscribe(clientObserver))
            {   
                var info = shell.ParseCommand("get", "-m", "node1");

                var tasks = new IChanneledMessageStreamer<Any>[] { _rpcClient, _rpcServer }
                   .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAny))
                   .ToArray();
                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(500));

                serverObserver.Received.Should().NotBeNull();
                serverObserver.Received.Payload.TypeUrl.Should().Be(GetMempoolRequest.Descriptor.ShortenedFullName());

                clientObserver.Received.Should().NotBeNull();
                clientObserver.Received.Payload.TypeUrl.Should().Be(GetMempoolResponse.Descriptor.ShortenedFullName());
            }
        
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) {return;}
            _scope?.Dispose();
            _rpcServer?.Dispose();
            _rpcClient?.Dispose();
        }
    }
}