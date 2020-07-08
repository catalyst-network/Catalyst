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
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Reactive.Testing;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Abstractions.P2P;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.Rpc.IO.Observers
{
    [TestFixture]
    [Category(Traits.IntegrationTest)]
    public sealed class GetInfoRequestObserverTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IConfigurationRoot _config;
        private readonly IPeerClient _peerClient;

        public GetInfoRequestObserverTests()
        {
            _testScheduler = new TestScheduler();
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, TestConstants.TestShellNodesConfigFile))
               .Build();

            _logger = Substitute.For<ILogger>();
            _peerClient = Substitute.For<IPeerClient>();
        }

        [Test]
        public async Task GetInfoMessageRequest_UsingValidRequest_ShouldSendGetInfoResponse()
        {
            var protocolMessage = new GetInfoRequest
            {
                Query = true
            }.ToProtocolMessage(MultiAddressHelper.GetAddress("sender"));

            var expectedResponseContent = JsonConvert
               .SerializeObject(_config.GetSection("CatalystNodeConfiguration").AsEnumerable(),
                    Formatting.Indented);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler,
                protocolMessage
            );

            var peerSettings = MultiAddressHelper.GetAddress("sender").ToSubstitutedPeerSettings();
            var handler = new GetInfoRequestObserver(
                peerSettings, _peerClient, _config, _logger);

            handler.StartObserving(messageStream);

            _testScheduler.Start();

            await _peerClient.Received(1).SendMessageAsync(Arg.Any<IMessageDto<ProtocolMessage>>());

            var receivedCalls = _peerClient.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1,
                "the only call should be the one we checked above");

            var response = ((IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments()[0])
               .Content.FromProtocolMessage<GetInfoResponse>();
            response.Query.Should().Match(expectedResponseContent,
                "the expected response should contain config information");
        }
    }
}
