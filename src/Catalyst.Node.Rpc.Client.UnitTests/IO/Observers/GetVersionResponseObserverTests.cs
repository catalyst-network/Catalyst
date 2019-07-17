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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;
using FluentAssertions;

namespace Catalyst.Node.Rpc.Client.UnitTests.IO.Observers
{
    public sealed class GetVersionResponseObserverTests : IDisposable
    {
        private readonly IUserOutput _output;
        public static readonly List<object[]> QueryContents;
        private readonly IChannelHandlerContext _fakeContext;

        private readonly ILogger _logger;
        private GetVersionResponseObserver _observer;

        static GetVersionResponseObserverTests()
        {
            QueryContents = new List<object[]>
            {
                new object[]
                {
                    "0.0.0.0"
                },
                new object[] {""}
            };
        }

        public GetVersionResponseObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        [Theory]
        [MemberData(nameof(QueryContents))]
        public async Task RpcClient_Can_Handle_GetVersionResponse(string version)
        {
            var response = new DtoFactory().GetDto(new VersionResponse
                {
                    Version = version
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                PeerIdentifierHelper.GetPeerIdentifier("recpient"),
                CorrelationId.GenerateCorrelationId()
            );

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext,
                response.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId,
                    response.CorrelationId)
            );

            VersionResponse messageStreamResponse = null;

            _observer = new GetVersionResponseObserver(_output, _logger);
            _observer.StartObserving(messageStream);
            _observer.Subscribe(message => messageStreamResponse = message);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            messageStreamResponse.Should().NotBeNull();
            messageStreamResponse.Version.Should().Be(response.Content.Version);
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
