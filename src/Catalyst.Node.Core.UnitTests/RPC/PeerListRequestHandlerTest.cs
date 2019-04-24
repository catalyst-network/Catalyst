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
using System.IO;
using System.Linq;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Common.Interfaces.P2P;
using SharpRepository.Repository;
using Catalyst.Common.P2P;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    /// <summary>
    /// Tests the peer list CLI and RPC calls
    /// </summary>
    /// <seealso cref="Catalyst.Common.UnitTests.TestUtils.ConfigFileBasedTest" />
    public sealed class PeerListRequestHandlerTest : ConfigFileBasedTest
    {
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IConfigurationRoot _config;
        private IContainer _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListRequestHandlerTest"/> class.
        /// </summary>
        /// <param name="output">The test output.</param>
        public PeerListRequestHandlerTest(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build(), CurrentTestName);

            ConfigureContainerBuilder(_config);

            _container = ContainerBuilder.Build();
            _scope = _container.BeginLifetimeScope(CurrentTestName);

            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        /// <summary>
        /// Tests the peer list request and response.
        /// </summary>
        /// <param name="fakePeers">The fake peers.</param>
        [Theory]
        [InlineData("127.0.0.1")]
        public void TestPeerListRequestResponse(params string[] fakePeers)
        {
            var peerRepository = Substitute.For<IRepository<Peer>>();

            var peerDiscovery = _container.Resolve<IPeerDiscovery>();

            fakePeers.ToList().ForEach(fakePeer =>
            {
                peerDiscovery.PeerRepository.Add(new Peer()
                {
                    LastSeen = DateTime.Now.Subtract(TimeSpan.FromSeconds(1)),
                    PeerIdentifier = new PeerIdentifier(PeerIdHelper.GetPeerId(fakePeer, "tc", 1, System.Net.IPAddress.Parse(fakePeer), 12345)),
                    Reputation = 0
                });
            });

            var request = new GetPeerListRequest().ToAnySigned(PeerIdHelper.GetPeerId("sender"), Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var subbedCache = Substitute.For<IMessageCorrelationCache>();
            var handler = new PeerListRequestHandler(PeerIdentifierHelper.GetPeerIdentifier("sender"), _logger, subbedCache, peerDiscovery);
            handler.StartObserving(messageStream);

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count().Should().Be(1);

            var sentResponse = (AnySigned) receivedCalls.Single().GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(GetPeerListResponse.Descriptor.ShortenedFullName());

            var responseContent = sentResponse.FromAnySigned<GetPeerListResponse>();

            responseContent.Peers.Count.Should().Be(fakePeers.Length);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            _scope?.Dispose();
        }
    }
}
