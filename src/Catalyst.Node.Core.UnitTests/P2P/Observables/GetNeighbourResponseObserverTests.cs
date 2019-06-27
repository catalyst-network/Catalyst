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
using System.Reactive.Linq;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Core.P2P.Observables;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.P2P.Observables
{
    public sealed class GetNeighbourResponseObserverTests : ConfigFileBasedTest
    {
        private readonly ILogger _logger;

        public GetNeighbourResponseObserverTests(ITestOutputHelper output) : base(output)
        {
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void CanResolveGetNeighbourResponseHandlerFromContainer()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
            
            ConfigureContainerBuilder(config, true, true);

            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var p2PMessageHandlers = container.Resolve<IEnumerable<IP2PMessageObserver>>();
                IEnumerable<IP2PMessageObserver> getNeighbourResponseHandler = p2PMessageHandlers.OfType<GetNeighbourResponseObserver>();
                getNeighbourResponseHandler.First().Should().BeOfType(typeof(GetNeighbourResponseObserver));
            }
        }

        [Fact(Skip = "This needs to be refactored as we don't hit rep cache here")] // @TODO 
        public void CanHandlerGetNeighbourRequestHandlerCorrectly()
        {
            var neighbourResponseHandler = new GetNeighbourResponseObserver(_logger);
            var peerNeighbourResponseMessage = new PeerNeighborsResponse();
            
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var channeledAny = new ProtocolMessageDto(fakeContext, peerNeighbourResponseMessage.ToProtocolMessage(PeerIdHelper.GetPeerId(), Guid.NewGuid()));
            var observableStream = new[] {channeledAny}.ToObservable();
            neighbourResponseHandler.StartObserving(observableStream);

            // neighbourResponseHandler.ReputableCache.ReceivedWithAnyArgs(1);
        }

        // [Fact]
        // public void PeerDiscoveryCanHandlePeerNeighbourMessageSubscriptions()
        // {
        //     var subbedPeerDiscovery = Substitute.For<IPeerDiscovery>();
        //     var peerNeighbourResponseMessage = new PeerNeighborsResponse();
        //     
        //     var fakeContext = Substitute.For<IChannelHandlerContext>();
        //     var channeledAny = new ProtocolMessageDto(fakeContext, peerNeighbourResponseMessage.ToProtocolMessage(PeerIdHelper.GetPeerId(), Guid.NewGuid()));
        //     var observableStream = new[] {channeledAny}.ToObservable();
        //     subbedPeerDiscovery.StartObserving(observableStream);
        //     subbedPeerDiscovery.GetNeighbourResponseStream.ReceivedWithAnyArgs(1);
        // }
    }
}
