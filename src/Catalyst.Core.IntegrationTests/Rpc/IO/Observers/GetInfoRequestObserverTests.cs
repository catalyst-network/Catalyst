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
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Reactive.Testing;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.IntegrationTests.Rpc.IO.Observers
{
    public sealed class GetInfoRequestObserverTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IConfigurationRoot _config;

        public GetInfoRequestObserverTests()
        {
            _testScheduler = new TestScheduler();
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Protocol.Common.Network.Devnet)))
               .Build();
            
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _fakeContext.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));
        }

        [Fact]
        public async Task GetInfoMessageRequest_UsingValidRequest_ShouldSendGetInfoResponse()
        {
            var protocolMessage = new GetInfoRequest
            {
                Query = true
            }.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId);

            var expectedResponseContent = JsonConvert
               .SerializeObject(_config.GetSection("CatalystNodeConfiguration").AsEnumerable(),
                    Formatting.Indented);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler,
                protocolMessage
            );
            
            var handler = new GetInfoRequestObserver(
                PeerIdentifierHelper.GetPeerIdentifier("sender"), _config, _logger);

            handler.StartObserving(messageStream);

            _testScheduler.Start();

            await _fakeContext.Channel.Received(1).WriteAndFlushAsync(Arg.Any<object>());

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1, 
                "the only call should be the one we checked above");

            var response = ((IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments()[0])
               .Content.FromProtocolMessage<GetInfoResponse>();
            response.Query.Should().Match(expectedResponseContent,
                "the expected response should contain config information");
        }
    }
}
