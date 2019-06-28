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
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Rpc.Client.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.Observables
{
    public sealed class VerifyMessageResponseObserverTests : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IUserOutput _output;

        public static List<object[]> QueryContents;

        private VerifyMessageResponseObserver _observer;

        static VerifyMessageResponseObserverTests()
        {
            QueryContents = new List<object[]>
            {
                new object[]
                {
                    true
                },
                new object[]
                {
                    false
                }
            };
        }

        public VerifyMessageResponseObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        [Theory]
        [MemberData(nameof(QueryContents))]
        public async Task RpcClient_Can_Handle_VerifyMessageResponse(bool isSignedByNode)
        {
            var response = new DtoFactory().GetDto(new VerifyMessageResponse
                {
                    IsSignedByKey = isSignedByNode
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, response.Message.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId, response.CorrelationId));

            _observer = new VerifyMessageResponseObserver(_output, _logger);
            _observer.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            _output.Received(1).WriteLine(isSignedByNode.ToString());
        }

        public void Dispose()
        {
            _observer.Dispose();
        }
    }
}
