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
using Catalyst.Cli.Rpc;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Cryptography;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Common.Interfaces.Rpc;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public sealed class RpcIntegrationTests : ConfigFileBasedTest
    {
        private const int MaxWaitInMs = 1000;
        private readonly IConfigurationRoot _config;

        public RpcIntegrationTests(ITestOutputHelper output) : base(output)
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
            ContainerBuilder.RegisterInstance(mempool).As<IMempool>();
            ContainerBuilder.RegisterType<NodeRpcClientFactory>().As<INodeRpcClientFactory>();
        }

        [Fact(Skip = "test hanger")]
        public void ServerConnectedToCorrectPort()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var rpcServer = container.Resolve<INodeRpcServer>();
                using (var client = new TcpClient(rpcServer.Settings.BindAddress.ToString(),
                    rpcServer.Settings.Port))
                {
                    client.Should().NotBeNull();
                    client.Connected.Should().BeTrue();
                }

                rpcServer.Dispose();
                scope.Dispose();
            }
        }

        [Fact(Skip = "test hanger")]
        public void RpcServer_Can_Handle_GetInfoRequest()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var rpcServer = container.Resolve<INodeRpcServer>();
                var logger = container.Resolve<ILogger>();
                var certificateStore = container.Resolve<ICertificateStore>();
                var nodeRpcClientFactory = container.Resolve<INodeRpcClientFactory>();

                var nodeRpcClient = nodeRpcClientFactory.GetClient(
                    certificateStore.ReadOrCreateCertificateFile("mycert.pfx"),
                    NodeRpcConfig.BuildRpcNodeSettingList(_config).FirstOrDefault()
                );

                var serverObserver = new AnySignedMessageObserver(0, logger);
                var clientObserver = new AnySignedMessageObserver(1, logger);

                using (rpcServer.MessageStream.Subscribe(serverObserver))
                {
                    using (nodeRpcClient.MessageStream.Subscribe(clientObserver))
                    {
                        var request = new GetInfoRequest();
                        var peerSettings = new PeerSettings(_config);
                        var pid = new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress, peerSettings.Port);
                        nodeRpcClient.SendMessage(request.ToAnySigned(pid.PeerId, Guid.NewGuid()));

                        var tasks = new IChanneledMessageStreamer<AnySigned>[]
                            {
                                nodeRpcClient, rpcServer
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

                rpcServer.Dispose();
                scope.Dispose();
            }
        }

        //[Fact(Skip = "test hanger")]
        [Fact(Skip = "test hanger")]
        public void RpcServer_Can_Handle_GetVersionRequest()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var rpcServer = container.Resolve<INodeRpcServer>();
                var logger = container.Resolve<ILogger>();
                var certificateStore = container.Resolve<ICertificateStore>();
                var nodeRpcClientFactory = container.Resolve<INodeRpcClientFactory>();

                var nodeRpcClient = nodeRpcClientFactory.GetClient(
                    certificateStore.ReadOrCreateCertificateFile("mycert.pfx"),
                    NodeRpcConfig.BuildRpcNodeSettingList(_config).FirstOrDefault()
                );
                var serverObserver = new AnySignedMessageObserver(0, logger);
                var clientObserver = new AnySignedMessageObserver(1, logger);

                using (rpcServer.MessageStream.Subscribe(serverObserver))
                {
                    using (nodeRpcClient.MessageStream.Subscribe(clientObserver))
                    {
                        var request = new VersionRequest();
                        var peerSettings = new PeerSettings(_config);
                        var pid = new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress, peerSettings.Port);
                        nodeRpcClient.SendMessage(request.ToAnySigned(pid.PeerId, Guid.NewGuid()));

                        var tasks = new IChanneledMessageStreamer<AnySigned>[]
                            {
                                nodeRpcClient, rpcServer
                            }
                           .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                           .ToArray();
                        Task.WaitAll(tasks, TimeSpan.FromMilliseconds(2000));

                        serverObserver.Received.Should().NotBeNull();
                        serverObserver.Received.Payload.TypeUrl.Should().Be(VersionRequest.Descriptor.ShortenedFullName());
                        clientObserver.Received.Should().NotBeNull();
                        clientObserver.Received.Payload.TypeUrl.Should().Be(VersionResponse.Descriptor.ShortenedFullName());
                    }
                }

                rpcServer.Dispose();
                scope.Dispose();
            }
        }

        [Fact(Skip = "test hanger")]
        public void RpcServer_Can_Handle_GetMempoolRequest()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var rpcServer = container.Resolve<INodeRpcServer>();
                var logger = container.Resolve<ILogger>();
                var certificateStore = container.Resolve<ICertificateStore>();
                var nodeRpcClientFactory = container.Resolve<INodeRpcClientFactory>();

                var nodeRpcClient = nodeRpcClientFactory.GetClient(
                    certificateStore.ReadOrCreateCertificateFile("mycert.pfx"),
                    NodeRpcConfig.BuildRpcNodeSettingList(_config).FirstOrDefault()
                );
                var serverObserver = new AnySignedMessageObserver(0, logger);
                var clientObserver = new AnySignedMessageObserver(1, logger);

                using (rpcServer.MessageStream.Subscribe(serverObserver))
                using (nodeRpcClient.MessageStream.Subscribe(clientObserver))
                {
                    var request = new GetMempoolRequest();
                    var peerSettings = new PeerSettings(_config);
                    var pid = new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress, peerSettings.Port);
                    nodeRpcClient.SendMessage(request.ToAnySigned(pid.PeerId, Guid.NewGuid()));

                    var tasks = new IChanneledMessageStreamer<AnySigned>[]
                        {
                            nodeRpcClient, rpcServer
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
        }

        [Fact(Skip = "test hanger")]
        public void RpcServer_Can_Handle_SignMessageRequest()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var rpcServer = container.Resolve<INodeRpcServer>();
                var logger = container.Resolve<ILogger>();
                var certificateStore = container.Resolve<ICertificateStore>();
                var nodeRpcClientFactory = container.Resolve<INodeRpcClientFactory>();

                var nodeRpcClient = nodeRpcClientFactory.GetClient(
                    certificateStore.ReadOrCreateCertificateFile("mycert.pfx"),
                    NodeRpcConfig.BuildRpcNodeSettingList(_config).FirstOrDefault()
                );

                var serverObserver = new AnySignedMessageObserver(0, logger);
                var clientObserver = new AnySignedMessageObserver(1, logger);

                using (rpcServer.MessageStream.Subscribe(serverObserver))
                using (nodeRpcClient.MessageStream.Subscribe(clientObserver))
                {
                    var message = "lol";
                    var request = new SignMessageRequest();
                    var bytesForRlpEncoding = message.Trim('\"').ToBytesForRLPEncoding();
                    var encodedMessage = RLP.EncodeElement(bytesForRlpEncoding);

                    request.Message = encodedMessage.ToByteString();

                    var peerSettings = new PeerSettings(_config);
                    var pid = new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress, peerSettings.Port);
                    nodeRpcClient.SendMessage(request.ToAnySigned(pid.PeerId, Guid.NewGuid()));

                    var tasks = new IChanneledMessageStreamer<AnySigned>[]
                        {
                            nodeRpcClient, rpcServer
                        }
                       .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                       .ToArray();
                    Task.WaitAll(tasks, TimeSpan.FromMilliseconds(MaxWaitInMs));

                    serverObserver.Received.Should().NotBeNull();
                    serverObserver.Received.Payload.TypeUrl.Should().Be(SignMessageRequest.Descriptor.ShortenedFullName());

                    clientObserver.Received.Should().NotBeNull();
                    clientObserver.Received.Payload.TypeUrl.Should().Be(SignMessageResponse.Descriptor.ShortenedFullName());
                }

                rpcServer.Dispose();
                scope.Dispose();
            }
        }
    }
}
