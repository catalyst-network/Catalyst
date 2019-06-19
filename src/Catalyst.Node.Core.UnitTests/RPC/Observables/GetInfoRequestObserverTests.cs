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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Core.RPC.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.RPC.Observables
{
    public sealed class GetInfoRequestObserverTests
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IConfigurationRoot _config;
        private readonly IRpcServerSettings _rpcServerSettings;

        public GetInfoRequestObserverTests()
        {
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
            
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _fakeContext.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));

            _rpcServerSettings = Substitute.For<IRpcServerSettings>();
            _rpcServerSettings.NodeConfig.Returns(_config);
        }

        [Fact]
        public async Task GetInfoMessageRequest_UsingValidRequest_ShouldSendGetInfoResponse()
        {
            var messageFactory = new ProtocolMessageFactory();
            var request = messageFactory.GetMessage(new MessageDto(
                new GetInfoRequest
                {
                    Query = true
                },
                MessageTypes.Request,
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                PeerIdentifierHelper.GetPeerIdentifier("sender")
            ));

            var expectedResponseContent = JsonConvert
               .SerializeObject(_config.GetSection("CatalystNodeConfiguration").AsEnumerable(),
                    Formatting.Indented);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var handler = new GetInfoRequestObserver(
                PeerIdentifierHelper.GetPeerIdentifier("sender"), _rpcServerSettings, _logger);

            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();

            await _fakeContext.Channel.Received(1).WriteAndFlushAsync(Arg.Any<object>());

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1, 
                "the only call should be the one we checked above");

            var response = ((IMessageDto) receivedCalls.Single().GetArguments()[0])
               .FromIMessageDto<GetInfoResponse>();
            response.Query.Should().Match(expectedResponseContent,
                "the expected response should contain config information");
        }
    }
}
