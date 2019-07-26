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
using System.Text;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Multiformats.Hash;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.IO.Observers
{
    public sealed class GetDeltaResponseObserverTests
    {
        private readonly GetDeltaResponseObserver _observer;
        private readonly Multihash _previousDeltaHash;

        public GetDeltaResponseObserverTests()
        {
            var logger = Substitute.For<ILogger>();
            _observer = new GetDeltaResponseObserver(logger);
            var hashingAlgorithm = Common.Config.Constants.HashAlgorithm;
            _previousDeltaHash = Encoding.UTF8.GetBytes("previous").ComputeMultihash(hashingAlgorithm);
        }

        [Fact]
        public async Task GetDeltaResponseObserver_Can_Output_Delta_As_Json()
        {
            var deltaContent = DeltaHelper.GetDelta(_previousDeltaHash);
            var deltaResponse = new GetDeltaResponse {Delta = deltaContent};

            var messageStream = CreateMessageStream(deltaResponse);

            GetDeltaResponse messageStreamResponse = null;
            _observer.StartObserving(messageStream);
            _observer.SubscribeToResponse(message => messageStreamResponse = message);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            messageStreamResponse.Should().NotBeNull();
            messageStreamResponse.ToJsonString().Should().Be(deltaResponse.ToJsonString());
        }

        [Fact]
        public async Task GetDeltaResponseObserver_Treats_Null_Content_As_Not_Found()
        {
            var deltaResponse = new GetDeltaResponse {Delta = null};

            var messageStream = CreateMessageStream(deltaResponse);

            GetDeltaResponse messageStreamResponse = null;
            _observer.StartObserving(messageStream);
            _observer.SubscribeToResponse(message => messageStreamResponse = message);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            messageStreamResponse.Should().NotBeNull();
            messageStreamResponse.Delta.Should().BeNull();
        }

        private static IObservable<IObserverDto<ProtocolMessage>> CreateMessageStream(GetDeltaResponse deltaResponse)
        {
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(Substitute.For<IChannelHandlerContext>(),
                deltaResponse.ToProtocolMessage(
                    PeerIdHelper.GetPeerId("sender"),
                    CorrelationId.GenerateCorrelationId()));
            return messageStream;
        }
    }
}

